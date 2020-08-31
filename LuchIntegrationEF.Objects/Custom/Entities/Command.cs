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
using LuchIntegrationEF.Objects.Custom.Enums;

namespace LuchIntegrationEF.Objects.Custom.Entities
{
    public partial class Command {

        public Command()
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
        
        public virtual string Query
        {
            get;
            set;
        }

        public virtual string Response
        {
            get;
            set;
        }

        public virtual CommandStatus Status
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
        public virtual Guid CommandTypeId
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

        [ForeignKey(nameof(CommandTypeId))]
        public virtual CommandType CommandType
        {
            get;
            set;
        }

        public virtual Guid Creator
        {
            get; 
            set;
        }
        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}