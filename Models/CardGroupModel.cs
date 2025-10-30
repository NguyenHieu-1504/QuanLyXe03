using System;

namespace QuanLyXe03.Models
{
    public class CardGroupModel
    {
        public Guid CardGroupID { get; set; }
        public string CardGroupCode { get; set; } = "";
        public string CardGroupName { get; set; } = "";
        public string Description { get; set; } = "";
        public int CardType { get; set; }
        public bool Inactive { get; set; }
    }
}