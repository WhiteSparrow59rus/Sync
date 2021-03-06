﻿//------------------------------------------------------------------------------
// This is auto-generated code.
//------------------------------------------------------------------------------
// This code was generated by Entity Developer tool using EF Core template.
// ''
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuchIntegrationEF.Objects.Custom.Entities
{
    public partial class Channel {

        public Channel()
        {
            this.Consumptions = new List<Consumption>();
            this.Indications = new List<Indication>();
            this.Calibrations = new List<Calibration>();
            OnCreated();
        }

        [System.ComponentModel.DataAnnotations.Key]
        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid Id
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual DateTimeOffset DateStart
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual DateTimeOffset DateFinish
        {
            get;
            set;
        }

        public virtual string Name
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual bool Logical
        {
            get;
            set;
        }

        public virtual int? Number
        {
            get;
            set;
        }

        public virtual int? BackEndId
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

        public virtual Guid? PhysicalChannelId
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid UnitId
        {
            get;
            set;
        }

        [ForeignKey(nameof(PhysicalChannelId))]
        public virtual Channel PhysicalChannel
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

        public virtual IList<Consumption> Consumptions
        {
            get;
            set;
        }

        [ForeignKey(nameof(UnitId))]
        public virtual Unit Unit
        {
            get;
            set;
        }

        public virtual IList<Indication> Indications
        {
            get;
            set;
        }

        public virtual IList<Calibration> Calibrations
        {
            get;
            set;
        }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
