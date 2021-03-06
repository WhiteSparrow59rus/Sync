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
    public partial class CommandType {

        public CommandType()
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

        public virtual string Description
        {
            get;
            set;
        }

        public virtual string Command
        {
            get;
            set;
        }

        public virtual byte[] Code
        {
            get;
            set;
        }

        public virtual string Parameters
        {
            get;
            set;
        }
        
        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid ProtocolId
        {
            get;
            set;
        }

        [ForeignKey(nameof(ProtocolId))]
        public virtual Protocol Protocol
        {
            get;
            set;
        }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
