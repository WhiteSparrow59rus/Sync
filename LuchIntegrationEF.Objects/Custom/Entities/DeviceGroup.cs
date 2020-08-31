using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuchIntegrationEF.Objects.Custom.Entities
{
    public partial class DeviceGroup
    {
        public DeviceGroup()
        {
        }

        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid Id
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual string Name
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual string Abbreviation
        {
            get;
            set;
        }

        public virtual DateTimeOffset TimeStampCreate
        {
            get;
            set;
        }

        public virtual Guid ContractorId
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid ResourceTypeId
        {
            get;
            set;
        }


        [ForeignKey(nameof(ContractorId))]
        public virtual Contractor Contractor
        {
            get;
            set;
        }

        [ForeignKey(nameof(ResourceTypeId))]
        public virtual DeviceType ResourceType
        {
            get;
            set;
        }

        [NotMapped]
        public virtual int CountDevices
        {
            get;
            set;
        }
    }
}
