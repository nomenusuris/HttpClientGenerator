using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace HttpClient.Utils
{
    public static class FormHelper
    {
        public static MultipartFormDataContent ToForm(object obj)
        {
            var formData = new MultipartFormDataContent();
            if (obj == null)
            {
                return null;
            }
            var objType = obj.GetType();
            if (typeof(IFormFile).IsAssignableFrom(objType))
            {
                var formFile = (IFormFile)obj;
                StreamContent fileContent = new StreamContent(formFile.OpenReadStream());
                formData.Add(fileContent, formFile.Name, formFile.FileName);
            }
            else if (typeof(IFormFileCollection).IsAssignableFrom(objType))
            {
                var formFiles = (IFormFileCollection)obj;
                foreach (var formFile in formFiles)
                {
                    StreamContent fileContent = new StreamContent(formFile.OpenReadStream());
                    formData.Add(fileContent, formFile.Name, formFile.FileName);
                }
            }
            else
            {
                var queryCollection = QuerySerializer.Serialize(obj, null);
                foreach (var queryField in queryCollection)
                {
                    foreach (var strVal in queryField.Value)
                    {
                        if (strVal != null)
                        {
                            formData.Add(new StringContent(strVal), queryField.Key);
                        }
                    }
                }
            }
            return formData;
        }
    }
}
