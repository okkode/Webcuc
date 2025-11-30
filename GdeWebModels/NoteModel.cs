using Swashbuckle.AspNetCore.Annotations;
using System;

namespace GdeWebModels
{
    [SwaggerSchema("Jegyzet osztálya")]
    public class NoteModel
    {
        [SwaggerSchema("Jegyzet azonosítója")]
        public int NoteId { get; set; } = 0;

        [SwaggerSchema("Felhasználó azonosítója")]
        public int UserId { get; set; } = 0;

        [SwaggerSchema("Kurzus azonosítója")]
        public int CourseId { get; set; } = 0;

        [SwaggerSchema("Jegyzet címe")]
        public string NoteTitle { get; set; } = String.Empty;

        [SwaggerSchema("Jegyzet tartalma")]
        public string NoteContent { get; set; } = String.Empty;

        [SwaggerSchema("Létrehozás dátuma")]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        [SwaggerSchema("Módosítás dátuma")]
        public DateTime ModificationDate { get; set; } = DateTime.Now;

        [SwaggerSchema("Művelet sikeressége")]
        public ResultModel Result { get; set; } = new ResultModel();
    }

    [SwaggerSchema("Jegyzet lista osztálya")]
    public class NoteListModel
    {
        [SwaggerSchema("Jegyzet lista")]
        public List<NoteModel> NoteList { get; set; } = new List<NoteModel>();

        [SwaggerSchema("Lista elemszáma")]
        public int Count { get; set; } = 0;

        [SwaggerSchema("Művelet sikeressége")]
        public ResultModel Result { get; set; } = new ResultModel();
    }
}

