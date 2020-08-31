using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LuchIntegrationEF.Objects.Custom.Enums;

namespace LuchIntegrationEF.Objects.Custom.Entities
{
    public class ContractorFacilities
    {
        [Key]
        [Required]
        public virtual Guid Id
        {
            get;
            set;
        }

        [Required]
        public Guid ContractorId { get; set; }

        [Required]
        public Guid FacilityId { get; set; }
        
        [Required]
        [ForeignKey(nameof(ContractorId))]
        public virtual Contractor Contractor
        {
            get;
            set;
        }

        [Required]
        [ForeignKey(nameof(FacilityId))]
        public virtual Facility Facility
        {
            get;
            set;
        }
    }
}
