using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuchIntegrationEF.Objects.Custom.Entities
{
    public partial class DeviceGroupsContent
    {
        public DeviceGroupsContent()
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
        public virtual Guid DeviceId
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid DeviceGroupId
        {
            get;
            set;
        }

        [ForeignKey(nameof(DeviceId))]
        public virtual Device Device
        {
            get;
            set;
        }

        [ForeignKey(nameof(DeviceGroupId))]
        public virtual DeviceGroup DeviceGroup
        {
            get;
            set;
        }

    }
}
