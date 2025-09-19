using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
//  inherits from identityuser which already includes id username email passwordhash securitystamp etc
//  adds fullname (required up to 80 chars) and dateofbirth (nullable datetime)
//  decorated with dataannotations for validation and display metadata
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(80)]
    public string FullName { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
}