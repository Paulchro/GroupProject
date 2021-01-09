﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using GroupProject.Models;
using System.Net;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;

namespace GroupProject.Controllers
{
    // OM: Commented out all unused scafolded code. TODO! Delete all comments after app is done and stable

    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        ApplicationDbContext context; // OM: added the database for index Action

        public ManageController()
        {
            context = new ApplicationDbContext(); // OM: added the database for index Action
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // OM: todo? Change Task<ActionResult> to simple Action?
        //
        // GET: /Manage/Index
        public ActionResult Index(ManageMessageId? message) 
        {
            // OM: todo! comment out unused messages
            ViewBag.StatusMessage = 
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var userId = User.Identity.GetUserId();

            // OM: to get user details from user with above id
            var user = context.Users.Find(userId); 

            // OM: TODO? dont know, i wanna try and see how this works
            //ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

            // OM: changed scaffolded viewmodel to get the things we want to show. Commented out unneeded stuff
            var model = new IndexViewModel 
            {
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                Created = user.Created,
                LastLogin = user.LastLog

                //HasPassword = HasPassword(),
                //PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                //TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                //Logins = await UserManager.GetLoginsAsync(userId),
                //BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            return View(model);
        }

        ////
        //// POST: /Manage/RemoveLogin
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        //{
        //    ManageMessageId? message;
        //    var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
        //    if (result.Succeeded)
        //    {
        //        var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //        if (user != null)
        //        {
        //            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        //        }
        //        message = ManageMessageId.RemoveLoginSuccess;
        //    }
        //    else
        //    {
        //        message = ManageMessageId.Error;
        //    }
        //    return RedirectToAction("ManageLogins", new { Message = message });
        //}

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        // OM: IMPORTANT! Layout doesn't refresh after Edit, so navbar will still show previous name unless user logs off. *Layout apparently reloads after logon/logoff
        [Authorize]
        public ActionResult Edit()
        {
            var userId = User.Identity.GetUserId();
            var user = context.Users.Find(userId);
            var model = new IndexViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserName, Email, FirstName, LastName, Address")] IndexViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.Identity.GetUserId();
                var user = context.Users.Find(userId);
                user.UserName = model.Username;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Address = model.Address;
                context.Entry(user).State = EntityState.Modified;
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        ////
        //// POST: /Manage/LinkLogin
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult LinkLogin(string provider)
        //{
        //    // Request a redirect to the external login provider to link a login for the current user
        //    return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        //}

        ////
        //// GET: /Manage/LinkLoginCallback
        //public async Task<ActionResult> LinkLoginCallback()
        //{
        //    var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
        //    if (loginInfo == null)
        //    {
        //        return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        //    }
        //    var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
        //    return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }
            base.Dispose(disposing);
        }

        // OM: Custom code from here on out. todo! Use code below to make UsersController for admin only

        //public ActionResult Users()
        //{

        //    ApplicationDbContext context = new ApplicationDbContext();
        //    var usersWithRoles = (from user in context.Users

        //                          select new
        //                          {
        //                              FirstName = user.FirstName,
        //                              LastName = user.LastName,
        //                              user.Roles,
        //                              user.LastLog,
        //                              Creation = user.Created,
        //                              UserId = user.Id,
        //                              Username = user.UserName,
        //                              user.Email,
        //                              RoleNames = (from userRole in user.Roles
        //                                           join role in context.Roles on userRole.RoleId
        //                                           equals role.Id
        //                                           select role.Name).ToList()
        //                          }).ToList().Select(p => new UserView()

        //                          {
        //                              //FirstName = p.FirstName,
        //                              //LastName = p.LastName,
        //                              //LastLogin = p.LastLog,
        //                              //Created = p.Creation,
        //                              UserId = p.UserId,
        //                              Username = p.Username,
        //                              Email = p.Email,
        //                              UserRoles = string.Join(",", p.RoleNames)

        //                          });


        //    return View(usersWithRoles);
        //}

        //public ActionResult Users()
        //{

        //    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

        //    return View(user);

        //}


        //[HttpGet]
        //public ActionResult Delete(string id)
        //{
        //    ApplicationDbContext context = new ApplicationDbContext();
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    var user = context.Users.Find(id);
        //    if (user == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(user);

        //}
        //[HttpPost]
        //[ActionName("Delete")]
        //public ActionResult DeleteConfirmed(string id)
        //{
        //    ApplicationDbContext context = new ApplicationDbContext();
        //    var userid = context.Users.Where(x => x.Id == id).Single();
        //    context.Users.Remove(userid);
        //    context.SaveChanges();
        //    return RedirectToAction("UsersWithRoles");
        //}
        //[HttpGet]
        //public ActionResult Edit(string id)
        //{
        //    ApplicationDbContext context = new ApplicationDbContext();
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    var user = context.Users.Find(id);

        //    if (user == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.Name = new SelectList(context.Roles.Where(u => !u.Name.Contains("Admin"))
        //                                .ToList(), "Name", "Name");
        //    return View(user);

        //}
        //[HttpPost, ActionName("Edit")]
        //[ValidateAntiForgeryToken]
        //public ActionResult EditPost(string id, ApplicationUser user)
        //{
        //    ApplicationDbContext context = new ApplicationDbContext();
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    var userToUpdate = context.Users.Find(id);
        //    //var userToUpdate = context.Users.SingleOrDefault(u => u.Id == user.Id);

        //    if (TryUpdateModel(userToUpdate, "",
        //       new string[] { "Email", "Username", "FirstName", "LastName" }))
        //    {
        //        try
        //        {
        //            context.SaveChanges();

        //            return RedirectToAction("UsersWithRoles");
        //        }
        //        catch (RetryLimitExceededException /* dex */)
        //        {
        //            //Log the error (uncomment dex variable name and add a line here to write a log.
        //            ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
        //        }
        //        ViewBag.Name = new SelectList(context.Roles.Where(u => !u.Name.Contains("Admin"))
        //                                 .ToList(), "Name", "Name");

        //    }

        //    return View(userToUpdate);
        //}

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}