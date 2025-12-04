#if DEBUG
using QuanLyXe03.Models;
using System;
using System.Collections.Generic;

namespace QuanLyXe03.ViewModels
{
    public class DesignCardEventManagementViewModel : CardEventManagementViewModel
    {
        public DesignCardEventManagementViewModel()
        {
            if (!Avalonia.Controls.Design.IsDesignMode) return;

            SearchText = "51H-12345";
            FromDate = DateTime.Today.AddDays(-3);
            ToDate = DateTime.Today;

            //  Bây giờ có thể gán vì setter là protected
            CardEvents = new List<CardEventModel>
            {
                new CardEventModel
                {
                    Index = 1,
                    CardNumber = "CARD001",
                    PlateIn = "51H-12345",
                    DatetimeIn = DateTime.Now.AddHours(-5),
                    DateTimeOut = DateTime.Now.AddHours(-3),
                    CustomerName = "Nguyễn Văn A",
                    Moneys = 75000
                },
                new CardEventModel
                {
                    Index = 2,
                    CardNumber = "CARD002",
                    PlateIn = "29A-67890",
                    DatetimeIn = DateTime.Now.AddHours(-2),
                    DateTimeOut = DateTime.Now.AddHours(-1),
                    CustomerName = "Trần Thị B",
                    Moneys = 45000
                }
            };

            TotalCount = CardEvents.Count;
        }
    }
}
#endif