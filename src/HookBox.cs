using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

/*namespace Extended
{
	/// <summary>
	/// 标记技能方法的特性，用于自动注册到 Hook 管道
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class HookAttribute : Attribute
	{
		/// <summary>执行顺序，数字越小越先执行</summary>
		public int Order;

		/// <summary>true=在 orig 前执行，false=在 orig 后执行</summary>
		public bool Before;

		/// <summary>所属 Hook 类型，如 typeof(HookTypes.PlayerUpdate)</summary>
		public Type HookType;

		/// <param name="hookType">Hook 类型标记</param>
		/// <param name="order">执行顺序</param>
		/// <param name="before">是否在 orig 前执行</param>
		public HookAttribute(Type hookType, int order, bool before = true)
		{
			Order = order;
			Before = before;
			HookType = hookType;
		}
	}

	/// <summary>
	/// Hook 类型标记，空类仅用于区分不同 Hook
	/// </summary>
	public static class HookTypes
	{
		public class Player_ctor { }


		/// <summary>对应 On.Player.Update</summary>
		public class Player_Update { }

		/// <summary>对应 On.Player.Die</summary>
		public class Player_Die { }

		/// <summary>对应 On.Spear.HitSomething</summary>
		public class Spear_Hit { }

		/// <summary>对应 On.Creature.Violence</summary>
		public class Creature_Violence { }
	}

	/// <summary>
	/// 注册中心：管理所有 Hook 盒子，提供扫描和运行功能
	/// </summary>
	public static class HookRegistry
	{
		// 存储所有已注册的盒子，key 是 HookType
		private static readonly Dictionary<Type, HookBox.Box> _boxes = new();

		/// <summary>
		/// 注册一个 Hook 盒子，必须在 Scan 之前调用
		/// </summary>
		/// <param name="hookType">Hook 类型标记，如 typeof(HookTypes.PlayerUpdate)</param>
		public static void Register(Type hookType)
		{
			_boxes[hookType] = new HookBox.Box();
		}

		/// <summary>
		/// 扫描程序集，自动注册所有带 [Hook] 特性的方法到对应盒子
		/// </summary>
		/// <param name="assembly">要扫描的程序集，通常用 Assembly.GetExecutingAssembly()</param>
		public static void Scan(Assembly assembly)
		{
			foreach (var type in assembly.GetTypes())
			{
				foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
				{
					// 读取 [Hook] 特性
					var attr = method.GetCustomAttribute < HookAttribute > ();
					if (attr == null || attr.HookType == null) continue;

					// 查找对应盒子
					if (!_boxes.TryGetValue(attr.HookType, out var box))
					{
						Debug.LogError($"未找到盒子: {attr.HookType.Name}");
						continue;
					}

					try
					{
						// 根据方法签名创建委托
						var del = CreateDelegate(method);
						// 添加到盒子，按 Order 排序
						box.Add(attr.Order, del, attr.Before);
					}
					catch (Exception e)
					{
						Debug.LogError($"注册失败: {method.Name}\n{e}");
					}
				}
			}
		}

		/// <summary>
		/// 根据方法签名自动创建委托类型
		/// void 方法 -> Action<<...>
		/// 有返回值 -> Func<<..., TReturn>
		/// </summary>
		private static Delegate CreateDelegate(MethodInfo method)
		{
			var parameters = method.GetParameters();
			// 收集所有参数类型
			var types = parameters.Select(p => p.ParameterType).ToList();

			Type delegateType;
			if (method.ReturnType == typeof(void))
			{
				// 无返回值：Action<T1, T2, ...>
				delegateType = Expression.GetActionType(types.ToArray());
			}
			else
			{
				// 有返回值：Func<T1, T2, ..., TReturn>
				types.Add(method.ReturnType);
				delegateType = Expression.GetFuncType(types.ToArray());
			}

			return Delegate.CreateDelegate(delegateType, method);
		}

		public static void SortAll()
		{
			foreach (var box in _boxes.Values)
				box.Sort();
		}

		/// <summary>
		/// 执行无返回值 Hook
		/// </summary>
		/// <param name="hookType">Hook 类型</param>
		/// <param name="orig">原始方法委托</param>
		/// <param name="args">原始方法的参数</param>
		public static void RunVoid(Type hookType, Delegate orig, params object[] args)
		{
			if (_boxes.TryGetValue(hookType, out var box))
				box.RunVoid(orig, args);
			else
				orig.DynamicInvoke(args);  // 没有注册就直调 orig
		}

		/// <summary>
		/// 执行有返回值 Hook
		/// </summary>
		/// <param name="hookType">Hook 类型</param>
		/// <param name="orig">原始方法委托</param>
		/// <param name="args">原始方法的参数</param>
		/// <returns>Hook 处理后的返回值</returns>
		public static object? RunReturn(Type hookType, Delegate orig, params object[] args)
		{
			if (_boxes.TryGetValue(hookType, out var box))
				return box.RunReturn(orig, args);
			return orig.DynamicInvoke(args);
		}
	}

	/// <summary>
	/// 核心盒子：存储并按顺序执行技能步骤
	/// 支持 orig 前/后执行，支持短路终止
	/// </summary>
	public static class HookBox
	{
		/// <summary>
		/// 具体盒子实现
		/// </summary>
		public class Box
		{
			// orig 前执行的步骤
			private List<Step> _before = new ();
			// orig 后执行的步骤
			private List<Step> _after = new ();

			private bool _sorted = false;  // 标记是否已排序


			/// <summary>
			/// 单个步骤
			/// </summary>
			private class Step
			{
				/// <summary>执行顺序</summary>
				public int Order;

				/// <summary>技能方法委托</summary>
				public Delegate Action = null!;
			}

			/// <summary>
			/// 添加技能到盒子
			/// </summary>
			/// <param name="order">执行顺序</param>
			/// <param name="action">技能方法委托</param>
			/// <param name="beforeOrig">true=orig前, false=orig后</param>
			public void Add(int order, Delegate action, bool beforeOrig = true)
			{
				var step = new Step { Order = order, Action = action };
				if (beforeOrig) _before.Add(step);
				else _after.Add(step);
				_sorted = false;  // 添加新元素后需要重新排序
			}

			// 注册完成后调用一次
			public void Sort()
			{
				if (_sorted) return;
				_before.Sort((a, b) => a.Order.CompareTo(b.Order));
				_after.Sort((a, b) => a.Order.CompareTo(b.Order));
				_sorted = true;
			}

			/// <summary>
			/// 执行无返回值管道
			/// 步骤返回 bool：true=继续执行，false=短路终止
			/// </summary>
			public void RunVoid(Delegate orig, params object[] args)
			{
				bool execute = true;  // 控制是否继续执行

				// ========== 1. orig 前步骤 ==========
				if (!_sorted) Sort();  // 兜底，但推荐外部调用
									   // 直接 foreach，不再 OrderBy
				foreach (var item in _before) //{  }
				//foreach (var item in _before.OrderBy(x => x.Order))
				{
					try
					{
						// 组装参数: (execute, orig, args...)
						var ret = item.Action.DynamicInvoke(
							new object[] { execute, orig }.Concat(args).ToArray()
						);
						// 如果返回 false，短路终止
						if (ret is bool b) execute = b;
					}
					catch (Exception e) { Debug.LogException(e); }
					if (!execute) return;  // 终止，不执行 orig
				}

				// ========== 2. 调用原始方法 ==========
				try { orig.DynamicInvoke(args); }
				catch (Exception e) { Debug.LogException(e); }

				// ========== 3. orig 后步骤 ==========
				foreach (var item in _after)
				//foreach (var item in _after.OrderBy(x => x.Order))
				{
					try
					{
						var ret = item.Action.DynamicInvoke(
							new object[] { execute, orig }.Concat(args).ToArray()
						);
						if (ret is bool b) execute = b;
					}
					catch (Exception e) { Debug.LogException(e); }
					if (!execute) return;
				}
			}

			/// <summary>
			/// 执行有返回值管道
			/// 支持返回值传递和修改
			/// </summary>
			public object? RunReturn(Delegate orig, params object[] args)
			{
				bool execute = true;
				object? result = null;  // 当前返回值

				// ========== 1. orig 前步骤 ==========
				if (!_sorted) Sort();  // 兜底，但推荐外部调用
									   // 直接 foreach，不再 OrderBy
				foreach (var item in _before) //{ }
				//foreach (var item in _before.OrderBy(x => x.Order))
				{
					try
					{
						// 组装参数，包含当前返回值
						var parameters = new List<object?> { execute, orig };
						parameters.AddRange(args);
						if (result != null) parameters.Add(result);

						var ret = item.Action.DynamicInvoke(parameters.ToArray());

						// 解析返回值
						if (ret is ValueTuple<bool, object> t)
						{
							execute = t.Item1;      // 是否继续
							result = t.Item2;       // 新返回值
						}
						else if (ret is bool b)
						{
							execute = b;            // 仅控制是否继续
						}
						else
						{
							result = ret;           // 新返回值
						}
					}
					catch (Exception e) { Debug.LogException(e); }
					if (!execute) return result;  // 短路返回
				}

				// ========== 2. 调用原始方法 ==========
				try { result = orig.DynamicInvoke(args); }
				catch (Exception e) { Debug.LogException(e); }

                // ========== 3. orig 后步骤 ==========
                foreach (var item in _after)
                //foreach (var item in _after.OrderBy(x => x.Order))
                {
                    try
					{
						var parameters = new List<object?> { execute, orig };
						parameters.AddRange(args);
						if (result != null) parameters.Add(result);

						var ret = item.Action.DynamicInvoke(parameters.ToArray());

						if (ret is ValueTuple<bool, object> t)
						{
							execute = t.Item1;
							result = t.Item2;
						}
						else
						{
							result = ret;
						}
					}
					catch (Exception e) { Debug.LogException(e); }
					if (!execute) return result;
				}

				return result;
			}
		}
	}
}*/
