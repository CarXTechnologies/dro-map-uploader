using System.Threading.Tasks;
using Modio.API.SchemaDefinitions;
using Modio.Errors;
using Newtonsoft.Json.Linq;

namespace Modio.API
{
    public static partial class ModioAPI
    {
        public static partial class Files
        {
            internal static async Task<(Error error, JToken modfileObject)> AddSourceModfileAsJToken(
                long modId,
                AddModfileRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/sources", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.MultipartFormData);

                request.Options.AddBody(body);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }
            
            internal static async Task<(Error error, ModfileObject? modfileObject)> AddSourceModfile(
                long modId,
                AddModfileRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/sources", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.MultipartFormData);

                request.Options.AddBody(body);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<ModfileObject>(request);
            }
        }
    }
}
