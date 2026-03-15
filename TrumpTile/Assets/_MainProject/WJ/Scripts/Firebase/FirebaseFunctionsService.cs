using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Firebase.Functions;

namespace TrumpTile.FirebaseLibrary
{
    public static class FirebaseFunctionsService
    {
        public static async Task RequestStartStage()
        {
            try
            {
                await FirebaseService.Functions.GetHttpsCallable(FirebaseFunctionsNames.START_STAGE).CallAsync();
            }
            catch (Exception e)
            {
                return;
            }
        }

        public static async Task<object> RequestLogin(string version)
        {
            try
            {
                HttpsCallableResult result = await FirebaseService.Functions.GetHttpsCallable(FirebaseFunctionsNames.PROGRESS_LOGIN).CallAsync(version);

                if (result == null)
                {
                    return null;
                }
                if(result.Data is string)
                {
                    return string.Empty;
                }
                return result.Data as Dictionary<object, object>;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public static async Task<Dictionary<object, object>> RequestEndStage()
        {
            return await RequestCallableFunctionHaveReturnValue(FirebaseFunctionsNames.END_STAGE);
        }
        public static async Task<Dictionary<object, object>> RequestPurchaseProduct()
        {
            return await RequestCallableFunctionHaveReturnValue(FirebaseFunctionsNames.PURCHASE_PRODUCT);
        }
        private static async Task<Dictionary<object, object>> RequestCallableFunctionHaveReturnValue(string functionName)
        {
            try
            {
                HttpsCallableResult result = await FirebaseService.Functions.GetHttpsCallable(functionName).CallAsync();

                if (result == null)
                {
                    return null;
                }
                return result.Data as Dictionary<object, object>;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
