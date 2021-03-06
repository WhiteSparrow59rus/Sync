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
    public partial class BalanceHistoryDetailed {

        public BalanceHistoryDetailed()
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
        public virtual DirectionCalc Direction
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual decimal Consumption
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual DateTimeOffset TimeStampStart
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual DateTimeOffset TimeStampFinish
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual DateTimeOffset TimeStampCalc
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid BalanceChannelId
        {
            get;
            set;
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public virtual Guid BalanceHistoryId
        {
            get;
            set;
        }

        [ForeignKey(nameof(BalanceChannelId))]
        public virtual BalanceChannel BalanceChannel
        {
            get;
            set;
        }

        [ForeignKey(nameof(BalanceHistoryId))]
        public virtual BalanceHistory BalanceHistory
        {
            get;
            set;
        }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
