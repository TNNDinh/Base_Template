using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets._Game._4.CORE.Modules.BugLogger
{
    /// <summary>
    ///     Service to fetch and cache employee list for tagging in bug reports
    /// </summary>
    public static class EmployeeService
    {
        #region Fields

        private const string API_URL = "https://m1.developer-a1f.workers.dev/api/get_employee";

        private static List<EmployeeInfoModel> _cachedEmployees;
        private static bool _isFetching;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets the list of employees, fetching from API if not cached
        /// </summary>
        public static async UniTask<List<EmployeeInfoModel>> GetEmployeesAsync()
        {
            if (_cachedEmployees != null && _cachedEmployees.Count > 0) return _cachedEmployees;

            if (_isFetching)
            {
                await UniTask.WaitUntil(() => !_isFetching);
                return _cachedEmployees;
            }

            _isFetching = true;

            try
            {
                using var req = UnityWebRequest.Get(API_URL);
                await req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var json = req.downloadHandler.text;
                    // Simple JSON array parsing. For production we might use Newtonsoft or a wrapper if it's a raw array.
                    // Unity's JsonUtility doesn't support top-level arrays directly without a wrapper.
                    _cachedEmployees = ParseJsonArray(json);
                }
                else
                {
                    Debug.LogError($"[EmployeeService] Failed to fetch employees: {req.error}");
                    _cachedEmployees = new List<EmployeeInfoModel>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EmployeeService] Exception during fetch: {ex.Message}");
                _cachedEmployees = new List<EmployeeInfoModel>();
            }
            finally
            {
                _isFetching = false;
            }

            return _cachedEmployees;
        }

        /// <summary>
        ///     Returns already cached employees or null if not fetched yet
        /// </summary>
        public static List<EmployeeInfoModel> GetCachedEmployees()
        {
            return _cachedEmployees;
        }

        #endregion

        #region Private Methods

        private static List<EmployeeInfoModel> ParseJsonArray(string json)
        {
            try
            {
                // Note: JsonUtility requires a wrapper for arrays. 
                // Using a common trick: wrap the array in an object string.
                var newJson = "{ \"Items\": " + json + " }";
                var wrapper = JsonUtility.FromJson<EmployeeListWrapper>(newJson);
                return wrapper?.Items ?? new List<EmployeeInfoModel>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EmployeeService] JSON Parse Error: {ex.Message}");
                return new List<EmployeeInfoModel>();
            }
        }

        [Serializable]
        private class EmployeeListWrapper
        {
            public List<EmployeeInfoModel> Items;
        }

        #endregion
    }
}