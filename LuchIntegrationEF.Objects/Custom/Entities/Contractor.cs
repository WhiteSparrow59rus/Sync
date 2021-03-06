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
    public partial class Contractor
    {

        public Contractor()
        {
            this.Children = new List<Contractor>();
            this.Reports = new List<Report>();
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

        public virtual string ShortName
        {
            get;
            set;
        }

        public virtual string LegalAddress
        {
            get;
            set;
        }

        public virtual string Email
        {
            get;
            set;
        }

        public virtual string ContactInformation
        {
            get;
            set;
        }

        public virtual Guid? OwnerId
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid ContractorRoleId
        {
            get;
            set;
        }

        public virtual IList<Contractor> Children
        {
            get;
            set;
        }

        [ForeignKey(nameof(OwnerId))]
        public virtual Contractor Owner
        {
            get;
            set;
        }
        
        public virtual IList<Report> Reports
        {
            get;
            set;
        }

        [ForeignKey(nameof(ContractorRoleId))]
        public virtual ContractorRole ContractorRole
        {
            get;
            set;
        }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
