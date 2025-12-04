using System;
using ReactiveUI;

namespace QuanLyXe03.Models
{
    /// <summary>
    /// Model cho nhóm khách hàng
    /// </summary>
    public class CustomerGroupModel : ReactiveObject
    {
        private Guid _customerGroupID;
        public Guid CustomerGroupID
        {
            get => _customerGroupID;
            set => this.RaiseAndSetIfChanged(ref _customerGroupID, value);
        }

        private string _customerGroupName = "";
        public string CustomerGroupName
        {
            get => _customerGroupName;
            set => this.RaiseAndSetIfChanged(ref _customerGroupName, value);
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        // Display text cho ComboBox
        public override string ToString()
        {
            return CustomerGroupName;
        }
    }
}