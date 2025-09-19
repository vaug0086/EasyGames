// Licensed to the .NET SCPFoundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

//  manage profile page identity and customfields
//
//  wtf is this anyways?
//  provides a pageModel for managing the user’s profile.
//  uses UserManager<ApplicationUser> to fetch/update the user's record
//  uses SignInManager<ApplicationUser> to refresh the authentication cookie after updates
//  InputModel with extra field  so they can be bound from form submissions and validated via DataAnnotations.
//  References 
//  Microsoft Docs ASP.NET Core Identity Manage user data, scaffolding Identity UI.
//  Microsoft Docs DataAnnotations for validation in MVC.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using EasyGames.Models;                         
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EasyGames.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;    
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required, StringLength(80)]
            [Display(Name = "Full name")]
            public string FullName { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Date of birth")]
            public DateTime? DateOfBirth { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Input = new InputModel
            {
                FullName = user.FullName ?? "",
                DateOfBirth = user.DateOfBirth,
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            
            user.FullName = Input.FullName ?? "";
            user.DateOfBirth = Input.DateOfBirth;

            
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}


//             ..                     .''. KITTY KITTY KITTY KITTY KITTY KITTY
//          c  '.          _,,    .'    '
//             ,     , ___  7###. .'       '
//            .       `-'   ''|####;#'. .##, .
//            ,               '####.##,######.
//           .                 '  ' '' ,####.
//          '                           '##'. Horrible ascii art. really fell off ngl ngl
//         '                                .
//  __,,--.--                                '
//        '                                  '
//    .--|-        &&                        -'-'''-.._
//     |         '&              &&         '
//      __,-                       '&        -'---.
//    ''  ,                                   ;
//         .             kk.                 -.-,_
//          - _           ''KK              ;
//          ''--,_                        ,
//                ''--, __           __,,--'
//                        ''--..,--''     KITTY KITTY KITTY KITTY KITTY KITTY