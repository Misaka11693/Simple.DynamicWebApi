﻿//using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.AspNetCore.Mvc;
//using Simple.DynamicWebApi;
//using Simple.DynamicWebApiSample.Entities;

//namespace Simple.DynamicWebApiSample;

///// <summary>
///// 用户领域服务
///// </summary>
//[DynamicApi]
////[Route("api/[controller]")]
//public class UserAppService
//{
//    private static List<User> _users = new();

//    /// <summary>
//    /// 创建用户
//    /// </summary>
//    public string CreateUserInfoAsync(User user)
//    {
//        _users.Add(user);
//        return "创建成功";
//    }

//    /// <summary>
//    /// 获取用户信息
//    /// </summary>
//    public string GetAsync(int id)
//    {
//        var user = _users.FirstOrDefault(x => x.Id == id);
//        if (user == null) { return "用户不存在"; }
//        return $"用户信息：{user.Name}";
//    }
//}
