using HalconDotNet;
using System;

/*************************************************************************************
 *
 * 文 件 名:   VisionEngine2
 * 描    述:
 *
 * 版    本：  V1.0.0.0
 * 创 建 者：  Bing
 * 创建时间：  2022/4/2 10:10:02
 * ======================================
 * 历史更新记录
 * 版本：V          修改时间：         修改人：
 * 修改内容：
 * ======================================
*************************************************************************************/

namespace BingLibrary.Vision.Engine
{
    /// <summary>
    /// 全局引擎
    /// </summary>
    public static class HalEngine
    {
        private static readonly Lazy<HDevEngine> _engine = new Lazy<HDevEngine>(() => new HDevEngine());
        public static HDevEngine Engine => _engine.Value;
    }

    /// <summary>
    /// 脱离 program，直接调用 procedure，推荐使用。
    /// </summary>
    public sealed class WorkerEngine : IDisposable
    {
        private readonly Dictionary<string, HDevProcedureCall> _devProcedureCalls = new Dictionary<string, HDevProcedureCall>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        /// <summary>
        /// 启用 JIT 编译
        /// </summary>
        /// <returns></returns>
        public bool EnableJIT()
        {
            try
            {
                HalEngine.Engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 获取所有过程名称
        /// </summary>
        public IReadOnlyCollection<string> ProcedureNames => _devProcedureCalls.Keys;

        /// <summary>
        /// 移除所有脚本
        /// </summary>
        public void RemoveAllProcedures()
        {
            foreach (var procedureCall in _devProcedureCalls.Values)
            {
                procedureCall.Dispose();
            }
            _devProcedureCalls.Clear();
        }

        /// <summary>
        /// 移除指定脚本
        /// </summary>
        /// <param name="name">脚本名称</param>
        public bool RemoveProcedure(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_devProcedureCalls.TryGetValue(name, out var procedureCall))
            {
                procedureCall.Dispose();
                return _devProcedureCalls.Remove(name);
            }
            return false;
        }

        /// <summary>
        /// 添加过程
        /// </summary>
        /// <param name="name">脚本名字，不包含后缀</param>
        /// <param name="path">脚本所在路径</param>
        /// <returns>是否添加成功</returns>
        public bool AddProcedure(string name, string path)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
                return false;

            if (_devProcedureCalls.ContainsKey(name))
                return false;

            try
            {
                HalEngine.Engine.SetProcedurePath(path);
                var procedure = new HDevProcedure(name);
                _devProcedureCalls.Add(name, new HDevProcedureCall(procedure));
                HalEngine.Engine.UnloadProcedure(name);
                return true;
            }
            catch (Exception ex)
            {
                // 记录日志
                System.Diagnostics.Debug.WriteLine($"Failed to add procedure {name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重新加载过程
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool ReloadProcedure(string name, string path)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
                return false;

            if (_devProcedureCalls.TryGetValue(name, out var procedureCall))
            {
                procedureCall.Dispose();
                _devProcedureCalls.Remove(name);
            }

            return AddProcedure(name, path);
        }

        /// <summary>
        /// 获取过程参数信息
        /// </summary>
        /// <param name="procedureName">过程名称</param>
        /// <returns>过程信息</returns>
        public ProcedureInfo GetProcedureInfo(string procedureName)
        {
            if (string.IsNullOrWhiteSpace(procedureName) || !_devProcedureCalls.TryGetValue(procedureName, out var procedureCall))
                return new ProcedureInfo();

            try
            {
                var procedure = procedureCall.GetProcedure();
                var info = new ProcedureInfo
                {
                    InputCtrlParamCount = procedure.GetInputCtrlParamCount(),
                    IntputIconicParamCount = procedure.GetInputIconicParamCount(),
                    OutputCtrlParamCount = procedure.GetOutputCtrlParamCount(),
                    OutputIconicParamCount = procedure.GetOutputIconicParamCount()
                };

                for (int i = 1; i <= info.InputCtrlParamCount; i++)
                {
                    info.InputCtrlParamNames.Add(procedure.GetInputCtrlParamName(i));
                }

                for (int i = 1; i <= info.IntputIconicParamCount; i++)
                {
                    info.InputIconicParamNames.Add(procedure.GetInputIconicParamName(i));
                }

                for (int i = 1; i <= info.OutputCtrlParamCount; i++)
                {
                    info.OutputCtrlParamNames.Add(procedure.GetOutputCtrlParamName(i));
                }

                for (int i = 1; i <= info.OutputIconicParamCount; i++)
                {
                    info.OutputIconicParamNames.Add(procedure.GetOutputIconicParamName(i));
                }

                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get procedure info for {procedureName}: {ex.Message}");
                return new ProcedureInfo();
            }
        }

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="procedureName">过程名称</param>
        /// <param name="paramName">参数名称</param>
        /// <param name="paramValue">参数值</param>
        /// <returns>是否设置成功</returns>
        public bool SetParam<T>(string procedureName, string paramName, T paramValue)
        {
            if (string.IsNullOrWhiteSpace(procedureName) || string.IsNullOrWhiteSpace(paramName))
                return false;

            if (!_devProcedureCalls.TryGetValue(procedureName, out var procedureCall))
                return false;

            try
            {
                switch (paramValue)
                {
                    case HImage image:
                    case HRegion region:
                        procedureCall.SetInputIconicParamObject(paramName, paramValue as HObject);
                        break;

                    case HTuple tuple:
                        procedureCall.SetInputCtrlParamTuple(paramName, tuple);
                        break;

                    case HDict dict:
                        procedureCall.SetInputCtrlParamTuple(paramName, dict);
                        break;

                    default:
                        procedureCall.SetInputCtrlParamTuple(paramName, new HTuple(paramValue));
                        return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set parameter {paramName} for procedure {procedureName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="procedureName">过程名称</param>
        /// <param name="paramName">参数名称</param>
        /// <returns>参数值</returns>
        public T GetParam<T>(string procedureName, string paramName) where T : class
        {
            if (string.IsNullOrWhiteSpace(procedureName) || string.IsNullOrWhiteSpace(paramName))
                return default;

            if (!_devProcedureCalls.TryGetValue(procedureName, out var procedureCall))
                return default;

            try
            {
                if (typeof(T) == typeof(HImage) || typeof(T) == typeof(HRegion))
                {
                    return procedureCall.GetOutputIconicParamImage(paramName) as T;
                }
                else if (typeof(T) == typeof(HTuple))
                {
                    return procedureCall.GetOutputCtrlParamTuple(paramName) as T;
                }
                else if (typeof(T) == typeof(HDict))
                {
                    var result = procedureCall.GetOutputCtrlParamTuple(paramName);
                    return new HDict(result.H) as T;
                }
                return default;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get parameter {paramName} from procedure {procedureName}: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 执行过程
        /// </summary>
        /// <param name="procedureName">过程名称</param>
        /// <returns>是否执行成功</returns>
        public bool ExecuteProcedure(string procedureName)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                return false;

            if (!_devProcedureCalls.TryGetValue(procedureName, out var procedureCall))
                return false;

            try
            {
                procedureCall.Execute();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to execute procedure {procedureName}: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    RemoveAllProcedures();
                }
                _disposed = true;
            }
        }

        ~WorkerEngine()
        {
            Dispose(false);
        }
    }

    public sealed class ProcedureInfo
    {
        public int InputCtrlParamCount { get; set; }
        public int IntputIconicParamCount { get; set; }
        public int OutputCtrlParamCount { get; set; }
        public int OutputIconicParamCount { get; set; }

        public List<string> InputCtrlParamNames { get; } = new List<string>();
        public List<string> InputIconicParamNames { get; } = new List<string>();
        public List<string> OutputCtrlParamNames { get; } = new List<string>();
        public List<string> OutputIconicParamNames { get; } = new List<string>();

        // 添加只读视图属性
        public IReadOnlyList<string> InputCtrlParamNamesView => InputCtrlParamNames;

        public IReadOnlyList<string> InputIconicParamNamesView => InputIconicParamNames;
        public IReadOnlyList<string> OutputCtrlParamNamesView => OutputCtrlParamNames;
        public IReadOnlyList<string> OutputIconicParamNamesView => OutputIconicParamNames;
    }
}