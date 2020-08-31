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

namespace LuchIntegrationEF.Objects.Custom.Entities
{
    public partial class Protocol {

        public Protocol()
        {
            this.CommandTypes = new List<CommandType>();
            this.EventTypes = new List<EventType>();
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
        public virtual string Name
        {
            get;
            set;
        }

        public virtual IList<CommandType> CommandTypes
        {
            get;
            set;
        }

        public virtual IList<EventType> EventTypes
        {
            get;
            set;
        }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
