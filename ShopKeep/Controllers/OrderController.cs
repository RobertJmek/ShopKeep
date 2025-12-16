using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShopKeep.Models;

namespace ShopKeep.Controllers
{

    public class OrderController : Controller
    {
    private readonly AppDbContext db;

        public OrderController(AppDbContext context)
        {
            db = context;
        }

    }
}