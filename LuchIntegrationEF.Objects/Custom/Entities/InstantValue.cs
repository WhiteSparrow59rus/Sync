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
using System.ComponentModel.DataAnnotations.Schema;

namespace LuchIntegrationEF.Objects.Custom.Entities
{
    public partial class InstantValue {

        public InstantValue()
        {
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
        public virtual DateTimeOffset TimeStamp
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual decimal Value
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
        public virtual Guid DeviceId
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

        [ForeignKey(nameof(DeviceId))]
        public virtual Device Device
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

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
