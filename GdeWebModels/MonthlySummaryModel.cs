using Swashbuckle.AspNetCore.Annotations;
using System;

namespace GdeWebModels
{
    [SwaggerSchema("Havi összefoglaló osztálya")]
    public class MonthlySummaryModel
    {
        [SwaggerSchema("Összefoglaló azonosítója")]
        public int SummaryId { get; set; } = 0;

        [SwaggerSchema("Felhasználó azonosítója")]
        public int UserId { get; set; } = 0;

        [SwaggerSchema("Év")]
        public int Year { get; set; } = DateTime.Now.Year;

        [SwaggerSchema("Hónap (1-12)")]
        public int Month { get; set; } = DateTime.Now.Month;

        [SwaggerSchema("AI által generált összefoglaló")]
        public string Summary { get; set; } = String.Empty;

        [SwaggerSchema("Mit tanultál ebben a hónapban?")]
        public string WhatLearned { get; set; } = String.Empty;

        [SwaggerSchema("Mit mutattak be a diákok?")]
        public string WhatPresented { get; set; } = String.Empty;

        [SwaggerSchema("Létrehozás dátuma")]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        [SwaggerSchema("Módosítás dátuma")]
        public DateTime ModificationDate { get; set; } = DateTime.Now;

        [SwaggerSchema("Művelet sikeressége")]
        public ResultModel Result { get; set; } = new ResultModel();
    }

    [SwaggerSchema("Havi összefoglaló lista osztálya")]
    public class MonthlySummaryListModel
    {
        [SwaggerSchema("Összefoglaló lista")]
        public List<MonthlySummaryModel> SummaryList { get; set; } = new List<MonthlySummaryModel>();

        [SwaggerSchema("Lista elemszáma")]
        public int Count { get; set; } = 0;

        [SwaggerSchema("Művelet sikeressége")]
        public ResultModel Result { get; set; } = new ResultModel();
    }
}

