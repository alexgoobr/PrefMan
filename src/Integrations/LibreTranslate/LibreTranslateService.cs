using Newtonsoft.Json;
using PrefMan.Integrations.LibreTranslate.Models;
using System.Text;
using System.Web;

namespace PrefMan.Integrations.LibreTranslate
{
    public class LibreTranslateService
    {
        public async Task<string> TranslateTextAsync(string text, string source, string target)
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.PostAsync("https://libretranslate.com/translate", FormatBodyContent(text, source, target));
                var responseContent = await response.Content.ReadAsStringAsync();
                var deserializedResponse = JsonConvert.DeserializeObject<LibreTranslateResponse>(responseContent);
                return deserializedResponse?.TranslatedText ?? text;
            }
            catch (Exception ex)
            {
                return text;
            }
        }

        public string TranslateText(string text, string source, string target)
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = httpClient.PostAsync("https://libretranslate.com/translate", FormatBodyContent(text, source, target)).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                var deserializedResponse = JsonConvert.DeserializeObject<LibreTranslateResponse>(responseContent);
                return deserializedResponse?.TranslatedText ?? text;
            }
            catch (Exception ex)
            {
                return text;
            }
        }

        private StringContent FormatBodyContent(string text, string source, string target)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("q", text);
            queryString.Add("source", source);
            queryString.Add("target", target);
            queryString.Add("format", "text");

            return new StringContent(queryString.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
        }

    }
}