﻿using Microsoft.AspNetCore.Mvc;
using Simple.DynamicWebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWebApiSample.Samples
{
    [Route("api/[controller]")]
    public class SampleService : IDynamicWebApi
    {
        private static List<User> _users = new List<User>()
        {
            new User() { Name = "John", Age = 27, Email = "john@example.com" },
            new User() { Name = "Jane", Age = 25, Email = "jane@example.com" }
        };

        public string Get()
        {
            return "Hello, Dynamic Web API!";
        }

        public dynamic GetTestDataByAge(int? age)
        {
            return _users.Where(u => u.Age == age);
        }

        public dynamic GetTestDataByName(string? name)
        {
            return _users.Where(u => u.Name == name);
        }

        public User Post(User user)
        {
            _users.Add(user);
            return user;
        }

        [HttpGet("user-info")]
        public dynamic GetInfo()
        {
            return _users;
        }
    }

    public class User
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
    }
}
