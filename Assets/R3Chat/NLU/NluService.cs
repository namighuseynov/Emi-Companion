using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R3Chat.OpenAI;

namespace R3Chat.NLU
{
    public sealed class NluService : INluService
    {
        private readonly OpenAIClient _openai;

        public NluService(OpenAIClient openai)
        {
            _openai = openai;
        }

        private static readonly JObject NluSchema = JObject.Parse(@"
{
  ""type"": ""object"",
  ""additionalProperties"": false,
  ""required"": [""turn_id"",""intent"",""topic"",""sentiment"",""politeness"",""engagement"",""expectation"",""events"",""constraints""],
  ""properties"": {
    ""turn_id"": { ""type"": ""integer"" },
    ""intent"": { ""type"": ""string"" },
    ""topic"": { ""type"": ""string"" },
    ""sentiment"": { ""type"": ""number"" },
    ""politeness"": { ""type"": ""number"" },
    ""engagement"": { ""type"": ""number"" },
    ""expectation"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""required"": [""type"",""violation_score""],
      ""properties"": {
        ""type"": { ""type"": ""string"" },
        ""violation_score"": { ""type"": ""number"" }
      }
    },
    ""events"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""required"": [""type"",""intensity"",""evidence""],
        ""properties"": {
          ""type"": { ""type"": ""string"" },
          ""intensity"": { ""type"": ""number"" },
          ""evidence"": { ""type"": ""string"" }
        }
      }
    },
    ""constraints"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""required"": [""language"",""reply_length""],
      ""properties"": {
        ""language"": { ""type"": ""string"" },
        ""reply_length"": { ""type"": ""string"" }
      }
    }
  }
}
");

        public async Task<NluPacket> ExtractAsync(int turnId, string userText, CancellationToken ct)
        {
            // Messages input (мануально задаём)
            var input = new JArray
            {
                new JObject { ["role"] = "user", ["content"] = userText }
            };

            string instructions =
@"Ты — модуль NLU (извлечение смысла). 
Верни ТОЛЬКО JSON, соответствующий схеме.
Заполняй: intent, topic, sentiment [-1..1], politeness [0..1], engagement [0..1].
events: список событий (type/intensity/evidence). Если событий нет — events = [].
constraints: language='ru', reply_length='short' если не ясно.
Никаких пояснений, никакого текста вне JSON.";

            var (jsonText, raw) = await _openai.CompleteJsonSchemaAsync(
                inputMessages: input,
                instructions: instructions,
                jsonSchema: NluSchema,
                ct: ct,
                store: false
            );

            try
            {
                return JsonConvert.DeserializeObject<NluPacket>(jsonText);
            }
            catch (Exception ex)
            {
                // Если вдруг что-то пошло не так — покажем сырьё
                throw new Exception("JSON parse error: " + ex.Message + "\nRAW_OUTPUT_TEXT:\n" + jsonText);
            }
        }
    }
}
