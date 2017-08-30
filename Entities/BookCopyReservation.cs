using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace aspnetcore_spa.Entities
{
    public class BookCopyReservation : IAuditFields
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]        
        public string BookCopyReservationId { get; set; }
        public string BookCopyBarCode { get; set; }
        public string StudentBarCode { get; set; }
        public DateTime CheckedOutDate { get; set; }
        public DateTime? CheckedInDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}