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
    public class CategoryController : Controller
    {
    private readonly AppDbContext db;

    public CategoryController(AppDbContext context)
        {
        db = context;
        }
    }
}