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
using LuchIntegrationEF.Objects.Custom.Enums;

namespace LuchIntegrationEF.Objects.Custom.Entities
{
    public partial class ContractorRole {

        public ContractorRole()
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
        public virtual TypeСontractorRole Type
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
        public virtual string NameEng
        {
            get;
            set;
        }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
