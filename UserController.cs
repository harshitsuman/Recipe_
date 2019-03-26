using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RecpMgmtWebApi.Models;

namespace RecpMgmtWebApi.Controllers
{
	public class UserController : ApiController
	{

		private RcpMgmtConnString db = new RcpMgmtConnString();

		// GET: api/User/ValidateUser?userName="username"
		[HttpGet]
		[ActionName("ValidateUser")]
		public IHttpActionResult ValidateUser(string userName)
		{
			UserTbl userTbl = db.UserTbls.Where(x => x.UserName == userName).FirstOrDefault();

			if (userTbl.DeletedDate == null)
				return Ok(userTbl.UserId);
			else
				return Ok(-1);

		}

//==================================================================================
		// GET: api/User/ValidatePassword?UserId=5&password="password"
		[HttpGet]
		[ActionName("ValidatePassword")]
		public IHttpActionResult ValidatePassword(int UserId, string password)
		{
			var result = (from a in db.UserTbls
						  where a.UserId == UserId && a.UserPwd == password
						  select new { a.UserName, a.UserId }).ToList();
			if (result.Count > 0)
				return Ok(0);
			else
				return Ok(-1);
		}

//==================================================================================
		// GET: api/User/GetRoleTbls
		[ActionName("GetRoleTbls")]
		public IHttpActionResult GetRoleTbls()
		{
			var result = (from a in db.RoleTbls
						  select new { a.RoleId, a.RoleName }).ToList();
			return Ok(result);
		}

//==================================================================================
		// GET: api/User/GetPermission
		[HttpGet]
		[ActionName("GetPermission")]
		public IHttpActionResult GetPermission()
		{
			var result = (from a in db.PermissionTbls
						  select new { a.PermissionId, a.PermissionName });
			return Ok(result);
		}

//====================================================================================
		// GET: api/User/GetAccessTbl
		[HttpGet]
		[ActionName("GetAccessTbl")]
		public IHttpActionResult GetAccessTbl()
		{
			var result = (from a in db.AccessTbls
						  select new { a.AccessId, a.AccessName });
			return Ok(result);
		}

//===================================================================================
		// GET: api/User/GetUser/5
		[HttpGet]
		[ActionName("GetUser")]
		public IHttpActionResult GetUserTbl(int id)
		{
			var result = (from a in db.UserTbls
						  where a.UserId == id
						  select new
						  {
							  a.UserName,
							  a.UserId,
							  a.FirstName,
							  a.LastName,
							  a.UserEmail,
							  a.UserPhone
						  }).ToList();
			UserDataModel userDataModel = new UserDataModel();
			userDataModel.UserId = result.FirstOrDefault().UserId;

			var result1 = (from a in db.UserRoleTbls
						   where a.UserId == id
						   select new { a.RoleId }).ToArray();

			if (result1 != null && result1.Count() > 0)
			{
				userDataModel.RoleId = new int[result1.Count()];
				for (int i = 0; i < result1.Count(); i++)
				{
					userDataModel.RoleId[i] = result1[i].RoleId;
				}
			}
			if (result.Count > 0)
				return Ok(userDataModel);
			else
				return Ok(-1);
		}

//====================================================================================
		// GET: api/User/GetUserTbls
		[HttpGet]
		[ActionName("GetUserTbls")]
		public IHttpActionResult GetUserTbls()
		{
			List<UserTbl> userTbls = new List<UserTbl>();
			var result = (from a in db.UserTbls
						  where a.DeletedDate.Equals(null)
						  select new
						  {
							  a.UserId,
							  a.UserName,
							  a.UserPwd,
							  a.FirstName,
							  a.LastName,
							  a.UserEmail,
							  a.UserPhone
						  }).ToList();
			return Ok(result);
		}

//====================================================================================

		// POST: api/User/AddUserRoleTbl
		[HttpPost]
		[ActionName("AddUserRoleTbl")]
		public HttpResponseMessage AddUserRoleTbl(UserDataModel userDataModal)
		{
			try
			{
				UserTbl userTbl = new UserTbl();
				userTbl.UserName = userDataModal.UserName;
				userTbl.UserPwd = userDataModal.UserPwd;
				userTbl.LastName = userDataModal.LastName;
				userTbl.FirstName = userDataModal.FirstName;
				userTbl.UserEmail = userDataModal.UserEmail;
				userTbl.UserPhone = userDataModal.UserPhone;

				int[] roleId = userDataModal.RoleId;
				db.UserTbls.Add(userTbl);
				db.SaveChanges();

				int latestUserId = userTbl.UserId;

				foreach (int items in roleId)
				{
					UserRoleTbl userRoleTbl = new UserRoleTbl();
					userRoleTbl.UserId = latestUserId;
					userRoleTbl.RoleId = items;
					db.UserRoleTbls.Add(userRoleTbl);

				}
				db.SaveChanges();
				var message = Request.CreateResponse(HttpStatusCode.Created, userTbl);
				return message;
			}
			catch (Exception ex)
			{
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
			}
		}
//==================================================================================
		// POST: api/User/DeleteUser
		[HttpPost]
		[ActionName("DeleteUser")]
		public HttpResponseMessage DeleteUser(UserDataModel userDataModel)
		{
			try
			{
				UserRoleTbl userRoleTbl = new UserRoleTbl();
				int id = userDataModel.UserId;
				var data = db.UserRoleTbls.FirstOrDefault(x => x.UserId == id);
				if (data == null)
				{
					return Request.CreateErrorResponse(HttpStatusCode.NotFound,
						"UserRoleTbl with userid = " + id.ToString() + " not found to delete");
				}
				else
				{
					List<UserRoleTbl> myUserRoleTbl = db.UserRoleTbls.Where(x => x.UserId == id).ToList();

					if (myUserRoleTbl.Count() > 0)
					{
						for (int j = 0; j < myUserRoleTbl.Count(); j++)
						{
							db.UserRoleTbls.Remove(myUserRoleTbl[j]);
						}
						db.SaveChanges();
					}
					UserTbl myUserTbl = db.UserTbls.Where(x => x.UserId == id).FirstOrDefault();
					myUserTbl.DeletedDate = DateTime.Now;
					db.SaveChanges();
					return Request.CreateResponse(HttpStatusCode.OK,
						"userid = " + id.ToString() + " is deleted successfully...!!");
				}
			}
			catch (Exception ex)
			{
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
			}
		}

//====================================================================================
		// Put: api/User/UpdateUser
		[HttpPut]
		[ActionName("UpdateUser")]
		public HttpResponseMessage UpdateUser(int id, UserDataModel userDataModel)
		{
			try
			{
				UserTbl userTbl = new UserTbl();
				var data = db.UserTbls.FirstOrDefault(x => x.UserId == id);
				if (data == null)
				{
					return Request.CreateErrorResponse(HttpStatusCode.NotFound,
						"UserTbl with userid = " + id.ToString() + " not found to update");
				}
				else
				{
					data.LastName = userDataModel.LastName;
					data.FirstName = userDataModel.FirstName;
					data.UserEmail = userDataModel.UserEmail;
					data.UserPhone = userDataModel.UserPhone;
					db.SaveChanges();

					List<UserRoleTbl> myUserRoleTbl = db.UserRoleTbls.Where(x => x.UserId == id).ToList();

					if (myUserRoleTbl.Count() > 0)
					{
						for (int j = 0; j < myUserRoleTbl.Count(); j++)
						{
							db.UserRoleTbls.Remove(myUserRoleTbl[j]);
						}
						db.SaveChanges();
					}

					int[] roleId = userDataModel.RoleId;
					int id1 = id;

					foreach (int items in roleId)
					{
						UserRoleTbl userRoleTbl = new UserRoleTbl();
						userRoleTbl.UserId = id1;
						userRoleTbl.RoleId = items;
						db.UserRoleTbls.Add(userRoleTbl);

					}
					db.SaveChanges();
					return Request.CreateResponse(HttpStatusCode.Created, data);
				}
			}
			catch (Exception ex)
			{
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
			}
		}

//====================================================================================
		//POST: api/User/AddTempAccessPermissionTbl
		[HttpPost]
		[ActionName("AddTempAccessPermissionTbl")]
		public IHttpActionResult AddTempAccessPermissionTbl(AccessPermissionTbl accessPermission)
		{
			try
			{
				int userId = 0;

				var result4 = (from a in db.UserTbls
							   where a.UserId.Equals(accessPermission.UserId)
							   select new
							   {
								   a.UserId,
								   a.DeletedDate
							   });
				if (result4.Count() > 0)
				{
					for (int i = 0; i < result4.Count(); i++)
					{
						if (result4.ToList()[i].DeletedDate == null && result4.ToList()[i].UserId == accessPermission.UserId)
						{
							userId = result4.ToList()[i].UserId;
						}
						else
						{
							return Content(HttpStatusCode.NotFound, "This user account is not null else not there in UserTbl");
						}
					}
				}
				else if (result4.Count() == 0)
				{
					return Content(HttpStatusCode.NotFound, "Userid is not exists");
				}

				if (userId == accessPermission.UserId)
				{
					db.AccessPermissionTbls.Add(accessPermission);
					db.SaveChanges();
					var result = (from a in db.AccessPermissionTbls
								  where a.UserId.Equals(accessPermission.UserId) &&
								  a.PermissionId.Equals(accessPermission.PermissionId) &&
								  a.AccessId.Equals(accessPermission.AccessId)
								  select new
								  {
									  a.UserId,
									  a.AccessId,
									  a.PermissionId
								  });
					if (result.Count() > 0)
					{
						for (int i = 0; i < result.Count(); i++)
						{
							return Ok(result);
						}
					}
				}
				return Content(HttpStatusCode.BadRequest, "table is empty based on UserId");
			}
			catch (Exception)
			{
				return Content(HttpStatusCode.BadRequest, "UserId, AccessId and PermissionId this sequence is already exists");
			}
		}

//====================================================================================
		// POST: api/User/DelTempAccessPermissionTbl
		[HttpPost]
		[ActionName("DelTempAccessPermissionTbl")]
		public IHttpActionResult DelTempAccessPermissionTbl(AccessPermissionTbl accessPermission)
		{
			try
			{
				AccessPermissionTbl accessPermissiontbl = new AccessPermissionTbl();

				int userId = accessPermission.UserId;
				int accessId = accessPermission.AccessId;
				int permissionId = accessPermission.PermissionId;
				AccessPermissionTbl accessPermissionTbl1 = db.AccessPermissionTbls.Find(userId, accessId, permissionId);
				db.AccessPermissionTbls.Remove(accessPermissionTbl1);
				db.SaveChanges();

				return Ok(accessPermissionTbl1);
			}
			catch (Exception)
			{
				return Content(HttpStatusCode.NotFound, "userId, accessId, permissionId these type of combination is not there...");
			}
		}

//==================================================================================
		// Post: api/User/AddRoleTblAndRoleAccessPermissionTbl
		[HttpPost]
		[ActionName("AddRoleTblAndRoleAccessPermissionTbl")]
		public IHttpActionResult AddRoleTblAndAccessPermissionTbl(UserDataModel userDataModel)
		{
			int latestRoleId = 0;
			try
			{
				string rolename = userDataModel.RoleName;
				var result3 = (from a in db.RoleTbls
							   where a.RoleName.Equals(userDataModel.RoleName)
							   select new
							   {
								   a.RoleName,
								   a.RoleId
							   });
				if (result3.Count() > 0)
				{
					for (int i = 0; i < result3.Count(); i++)
					{
						latestRoleId = result3.ToList()[i].RoleId;
					}
				}

				else
				{
					RoleTbl roleTbl = new RoleTbl();
					roleTbl.RoleName = userDataModel.RoleName;

					db.RoleTbls.Add(roleTbl);
					db.SaveChanges();
					latestRoleId = roleTbl.RoleId;
				}
				int userId = 0;

				var result4 = (from a in db.UserTbls
							   where a.UserId.Equals(userDataModel.UserId)
							   select new
							   {
								   a.UserId,
								   a.DeletedDate
							   });
				if (result4.Count() > 0)
				{
					for (int i = 0; i < result4.Count(); i++)
					{
						if (result4.ToList()[i].DeletedDate == null && result4.ToList()[i].UserId == userDataModel.UserId)
						{
							userId = result4.ToList()[i].UserId;
						}
						else
						{
							return Content(HttpStatusCode.NotFound, "This user account is not null else not there in UserTbl");
						}
					}
				}
				else if (result4.Count() == 0)
				{
					return Content(HttpStatusCode.NotFound, "Userid is not exists");
				}

				var result = (from a in db.AccessPermissionTbls
							  where a.UserId.Equals(userId)
							  select new
							  {
								  a.AccessId,
								  a.PermissionId,
								  a.UserId
							  }).ToList();

				var result1 = (from a in db.RoleAccessPermissionTbls
							   where a.RoleId.Equals(latestRoleId)
							   select new
							   {
								   a.RoleId,
								   a.AccessId,
								   a.PermissionId
							   }).ToList();

				if (result.Count() > 0)
				{
					if (result1.Count() > 0)
					{
						for (int j = 0; j < result1.Count(); j++)
						{
							for (int i = 0; i < result.Count(); i++)
							{
								if (result.ToList()[i].AccessId == result1.ToList()[j].AccessId && result.ToList()[i].PermissionId == result1.ToList()[j].PermissionId)
								{
									AccessPermissionTbl accessPermissiontbl = new AccessPermissionTbl();
									int userid1 = userId;
									int access = result1.ToList()[j].AccessId;
									int permission = result1.ToList()[j].PermissionId;
									AccessPermissionTbl accessPermissionTbl1 = db.AccessPermissionTbls.Find(userid1, access, permission);
									db.AccessPermissionTbls.Remove(accessPermissionTbl1);
									db.SaveChanges();
									return Ok("RoleId, AccessId and PermissionId these combination are already there " +
										"and click save again if you have given morethen one Accessname and Permission name...");
								}
							}
						}
					}
				}

				if (result.Count() > 0)
				{
					for (int i = 0; i < result.Count(); i++)
					{
						RoleAccessPermissionTbl roleAccessPermissionTbl = new RoleAccessPermissionTbl();

						roleAccessPermissionTbl.AccessId = result.ToList()[i].AccessId;
						roleAccessPermissionTbl.PermissionId = result.ToList()[i].PermissionId;
						roleAccessPermissionTbl.RoleId = latestRoleId;
						db.RoleAccessPermissionTbls.Add(roleAccessPermissionTbl);
					}
					db.SaveChanges();
				}

				if (result.Count() > 0)
				{
					for (int j = 0; j < result.Count(); j++)
					{
						AccessPermissionTbl accessPermissiontbl = new AccessPermissionTbl();
						int userid1 = result.ToList()[j].UserId;
						int access = result.ToList()[j].AccessId;
						int permission = result.ToList()[j].PermissionId;
						AccessPermissionTbl accessPermissionTbl1 = db.AccessPermissionTbls.Find(userid1, access, permission);
						db.AccessPermissionTbls.Remove(accessPermissionTbl1);
					}
					db.SaveChanges();
				}
				else
				{
					return Content(HttpStatusCode.BadRequest, "insert record first in AccessPermissionTbl");
				}
				//db.SaveChanges();
				return Ok("Successfully Added");
			}
			catch (Exception ex)
			{
				return Content(HttpStatusCode.BadRequest, ex);
			}

		}

//===================================================================================
		// POST: api/User/UpdateAccessPermissionTbl
		[HttpPut]
		[ActionName("UpdateAccessPermissionTbl")]
		public IHttpActionResult UpdateAccessPermissionTbl(UserDataModel userDataModal)
		{
			try
			{
				int role = userDataModal.role;
				int userId = userDataModal.UserId;

				var result = (from a in db.AccessPermissionTbls
							  where a.UserId.Equals(userId)
							  select new
							  {
								  a.AccessId,
								  a.PermissionId,
								  a.UserId
							  }).ToList();

				var result1 = (from a in db.RoleAccessPermissionTbls
							   where a.RoleId.Equals(role)
							   select new
							   {
								   a.RoleId,
								   a.AccessId,
								   a.PermissionId
							   }).ToList();

				if (result.Count() > 0)
				{
					if (result1.Count() > 0)
					{
						for (int j = 0; j < result1.Count(); j++)
						{
							for (int i = 0; i < result.Count(); i++)
							{
								if (result.ToList()[i].AccessId == result1.ToList()[j].AccessId && result.ToList()[i].PermissionId == result1.ToList()[j].PermissionId)
								{
									AccessPermissionTbl accessPermissiontbl = new AccessPermissionTbl();
									int userid1 = userId;
									int access = result1.ToList()[j].AccessId;
									int permission = result1.ToList()[j].PermissionId;
									AccessPermissionTbl accessPermissionTbl1 = db.AccessPermissionTbls.Find(userid1, access, permission);
									db.AccessPermissionTbls.Remove(accessPermissionTbl1);
									db.SaveChanges();
									return Ok("RoleId, AccessId and PermissionId these combination is already there " +
										"and click save again if you have given morethen one Accessname and Permission name...");
								}
							}
						}
					}
				}

				if (result1.Count() > 0)
				{
					for (int i = 0; i < result1.Count(); i++)
					{
						AccessPermissionTbl accessPermissionTbl = new AccessPermissionTbl();
						accessPermissionTbl.AccessId = result1.ToList()[i].AccessId;
						accessPermissionTbl.PermissionId = result1.ToList()[i].PermissionId;
						accessPermissionTbl.UserId = userId;
						db.AccessPermissionTbls.Add(accessPermissionTbl);
					}
					db.SaveChanges();
				}

				else
				{
					return Content(HttpStatusCode.NotFound, "RoleId & UserId is not there");
				}
				var result10 = (from a in db.AccessPermissionTbls
								where a.UserId == userId
								select new { a.AccessId, a.PermissionId }).ToList();

				var result2 = (from b in db.AccessPermissionTbls
							   where b.UserId.Equals(userId)
							   select new
							   {
								   b.UserId,
								   b.AccessId,
								   b.PermissionId
							   }).ToList();

				if (result1.Count() > 0)
				{
					for (int i = 0; i < result1.Count(); i++)
					{
						RoleAccessPermissionTbl roleAccessPermissionTbl = new RoleAccessPermissionTbl();
						int roleid = result1.ToList()[i].RoleId;
						int accessid = result1.ToList()[i].AccessId;
						int permissionid = result1.ToList()[i].PermissionId;
						RoleAccessPermissionTbl roleAccessPermissionTbl1 = db.RoleAccessPermissionTbls.Find(roleid, accessid, permissionid);
						db.RoleAccessPermissionTbls.Remove(roleAccessPermissionTbl1);
					}
					db.SaveChanges();
				}

				if (result2.Count() > 0)
				{
					for (int i = 0; i < result2.Count(); i++)
					{
						RoleAccessPermissionTbl roleAccessPermissionTbl = new RoleAccessPermissionTbl();
						roleAccessPermissionTbl.RoleId = role;
						roleAccessPermissionTbl.AccessId = result2.ToList()[i].AccessId;
						roleAccessPermissionTbl.PermissionId = result2.ToList()[i].PermissionId;
						db.RoleAccessPermissionTbls.Add(roleAccessPermissionTbl);
					}
					db.SaveChanges();
				}

				if (result2.Count() > 0)
				{
					for (int i = 0; i < result2.Count(); i++)
					{
						AccessPermissionTbl accessPermissionTbl = new AccessPermissionTbl();
						int access = result2.ToList()[i].AccessId;
						int permission = result2.ToList()[i].PermissionId;
						int userid = userId;
						AccessPermissionTbl accessPermissionTbl1 = db.AccessPermissionTbls.Find(userid, access, permission);
						db.AccessPermissionTbls.Remove(accessPermissionTbl1);
					}
					db.SaveChanges();
				}
				return Ok(result10);
			}

			catch (Exception)
			{
				return Content(HttpStatusCode.NotFound, "RoleId & UserId already exists");
			}
		}

//===================================================================================
		// GET: api/User/GetPermissionName/10
		[HttpGet]
		[ActionName("GetPermissionName")]
		public IHttpActionResult GetPermissionName(int id)
		{
			int uid = id;
			var result = (from ut in db.UserTbls
						  join urt in db.UserRoleTbls on ut.UserId equals urt.UserId
						  join rapt in db.RoleAccessPermissionTbls on urt.RoleId equals rapt.RoleId
						  join pt in db.PermissionTbls on rapt.PermissionId equals pt.PermissionId
						  where ut.UserId.Equals(uid)
						  select new
						  {
							  pt.PermissionName
						  }).Distinct().ToList();
			if (result.Count() > 0)
			{
				string[] pname = new string[result.Count];
				int i = 0;

				foreach (var item in result)
				{
					pname[i] = item.PermissionName;
					i++;
				}
				return Ok(pname);
			}
			else
			{
				return Content(HttpStatusCode.BadRequest, "this userid has no permission");
			}
		}

//===========================================================================================
		// GET: api/User/GetAccessName?id=10&aName=Configuration
		[HttpGet]
		[ActionName("GetAccessName")]
		public IHttpActionResult GetAccessName(int id, string aName)
		{
			int uid = id;
			string permissionName = aName;

			var result = (from ut in db.UserTbls
						  join urt in db.UserRoleTbls on ut.UserId equals urt.UserId
						  join rapt in db.RoleAccessPermissionTbls on urt.RoleId equals rapt.RoleId
						  join pt in db.PermissionTbls on rapt.PermissionId equals pt.PermissionId
						  join at in db.AccessTbls on rapt.AccessId equals at.AccessId
						  where ut.UserId.Equals(uid) && pt.PermissionName.Equals(permissionName)
						  select new
						  {
							  at.AccessName
						  }).Distinct().ToList();

			if (result.Count() > 0)
			{
				string[] aname = new string[result.Count];
				int i = 0;

				foreach (var item in result)
				{
					aname[i] = item.AccessName;
					i++;
				}
				return Ok(aname);
			}
			else
			{
				return Content(HttpStatusCode.BadRequest, "this userid has no AccessName");
			}

		}

//===========================================================================================
		// Delete : api/User/DeleteTempAccessPermissionTblBasedOnUserId/5
		[HttpDelete]
		[ActionName("DeleteTempAccessPermissionTblBasedOnUserId")]
		public IHttpActionResult DeleteTempAccessPermissionTblBasedOnUserId(int id)
		{
			try
			{
				int uid = id;
				var result = (from a in db.AccessPermissionTbls
							  where a.UserId.Equals(uid)
							  select new
							  {
								  a.UserId,
								  a.AccessId,
								  a.PermissionId
							  }).ToList();

				if (result.Count() > 0)
				{
					for (int i = 0; i < result.Count(); i++)
					{
						AccessPermissionTbl accessPermissionTbl = new AccessPermissionTbl();
						int userid1 = result.ToList()[i].UserId;
						int access = result.ToList()[i].AccessId;
						int permission = result.ToList()[i].PermissionId;
						AccessPermissionTbl accessPermissionTbl1 = db.AccessPermissionTbls.Find(userid1, access, permission);
						db.AccessPermissionTbls.Remove(accessPermissionTbl1);
					}
					db.SaveChanges();
				}
				else
				{
					Console.WriteLine("Error");
					return Content(HttpStatusCode.NotFound, "User with id = " + uid.ToString() + " not found");
				}
				return Ok("Userid " + uid + " Successfully deleted");
			}
			catch(Exception)
			{
				return Content(HttpStatusCode.BadRequest, "User should give data in proper format");
			}
		}

//=======================================================================================
	}
}
