// Models/Emailing/CustomerGroup.cs
using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models.Emailing
{
    public class CustomerGroup
    {
        public int Id { get; set; }
        [Required, MaxLength(120)] public string Name { get; set; } = "";
        [MaxLength(500)] public string? Description { get; set; }
        public List<CustomerGroupMember> Members { get; set; } = new();
        public List<EmailCampaign> Campaigns { get; set; } = new();
    }

    public class CustomerGroupMember
    {
        public int Id { get; set; }
        [Required] public int GroupId { get; set; }
        public CustomerGroup Group { get; set; } = null!;
        [MaxLength(450)] public string? UserId { get; set; }
        [Required, EmailAddress, MaxLength(320)] public string Email { get; set; } = "";
        [MaxLength(200)] public string? DisplayName { get; set; }
        public bool Unsubscribed { get; set; } = false;
    }

    public enum CampaignStatus { Draft, Scheduled, Sending, Completed, Failed, Cancelled }
    public enum SendStatus { Queued, Sent, Failed, Bounced, Opened, Unsubscribed, Skipped }

    public class EmailCampaign
    {
        public int Id { get; set; }

        [Required, MaxLength(160)] public string Name { get; set; } = "";
        [Required, MaxLength(200)] public string Subject { get; set; } = "";

        // Authoring fields
        [Required] public string Details { get; set; } = "";          // plain-text email body
        // Given this assignment is not hosted and with blob storage, and is only for development at this stage,
        // image upload has not been made functional. Instead the user must provide links to existing publicly
        // available images. Long term this would be transitioned to a hosted blob storage like S3.
        [MaxLength(500)] public string? ImageUrl { get; set; }         // website-only
        [MaxLength(80)] public string? CtaText { get; set; }          // website-only
        [MaxLength(500)] public string? CtaLink { get; set; }          // website-only

        //  legacy preview field (not used for email sending)
        public string? BodyHtml { get; set; }

        [Required] public int GroupId { get; set; }
        public CustomerGroup Group { get; set; } = null!;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ScheduledUtc { get; set; }
        public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

        public bool IsPublic { get; set; } = false;
        public DateTime? PublishedUtc { get; set; }

        public List<EmailCampaignRecipient> Recipients { get; set; } = new();
    }

    public class EmailCampaignRecipient
    {
        public int Id { get; set; }
        [Required] public int CampaignId { get; set; }
        public EmailCampaign Campaign { get; set; } = null!;
        [Required, EmailAddress, MaxLength(320)] public string Email { get; set; } = "";
        [MaxLength(200)] public string? DisplayName { get; set; }
        public SendStatus Status { get; set; } = SendStatus.Queued;
        public DateTime? QueuedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SentUtc { get; set; }
        public DateTime? OpenedUtc { get; set; }
        [MaxLength(1000)] public string? LastError { get; set; }
        [MaxLength(500)] public string? BounceReason { get; set; }
        [MaxLength(200)] public string? ProviderMessageId { get; set; }
    }
}
