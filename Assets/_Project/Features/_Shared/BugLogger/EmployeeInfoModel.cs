using System;

namespace Assets._Game._4.CORE.Modules.BugLogger
{
    /// <summary>
    ///     Model class for employee data from API
    /// </summary>
    [Serializable]
    public class EmployeeInfoModel
    {
        public string id;
        public string name;
        public int type;
    }
}