﻿using Newtonsoft.Json;

namespace Test.Backend
{
    public class Account
    {
        public int Value { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        public string AccountNumber { get; set; }
    }
}