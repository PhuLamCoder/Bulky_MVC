using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
		}
		public IActionResult Index()
		{
			return View();
		}
		public IActionResult Upsert(int? id)
		{
			//IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category
			//	.GetAll().Select(category => new SelectListItem
			//	{
			//		Text = category.Name,
			//		Value = category.Id.ToString()
			//	});
			//ViewBag.CategoryList = CategoryList;
			ProductVM productVM = new ProductVM()
			{
				CategoryList = _unitOfWork.Category
						.GetAll().Select(category => new SelectListItem
						{
							Text = category.Name,
							Value = category.Id.ToString()
						})
			};

			if (id == null || id == 0)
			{
				productVM.Product = new Product();
				return View(productVM);
			}
			else
			{
				productVM.Product = _unitOfWork.Product.Get(product => product.Id == id);
				return View(productVM);
			}
		}

		[HttpPost]
		public async Task<IActionResult> Upsert(ProductVM productVM, IFormFile? file)
		{
			if (ModelState.IsValid)
			{
				string wwwRootPath = _webHostEnvironment.WebRootPath;
				if (file != null)
				{
					string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string productPath = Path.Combine(wwwRootPath, @"images/product");

					if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
					{
						// delete old image
						var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl);
						if (System.IO.File.Exists(oldImagePath))
						{
							System.IO.File.Delete(oldImagePath);
						}
					}

					using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
					{
						await file.CopyToAsync(fileStream);
					}

					productVM.Product.ImageUrl = @"images/product/" + fileName;
				}

				if (productVM.Product.Id == 0)
				{
					await _unitOfWork.Product.AddAsync(productVM.Product);
					TempData["success"] = "Product created successfully!";
				}
				else
				{
					_unitOfWork.Product.Update(productVM.Product);
					TempData["success"] = "Product updated successfully!";
				}

				await _unitOfWork.SaveAsync();
				return RedirectToAction("Index");
			}
			else
			{
				productVM.CategoryList = _unitOfWork.Category
						.GetAll().Select(category => new SelectListItem
						{
							Text = category.Name,
							Value = category.Id.ToString()
						});
				return View(productVM);
			}
		}

		#region API CALLS

		[HttpGet]
		public IActionResult GetAll()
		{
			List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
			return Json(new {data = objProductList});
		}

		[HttpDelete]
		public async Task<IActionResult> Delete(int? id)
		{
			var productToBeDeleted = _unitOfWork.Product.Get(product => product.Id == id);
			if (productToBeDeleted == null)
			{
				return Json(new { success = false, message = "Error while deleting!" });
			}
			var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl);
			if (System.IO.File.Exists(oldImagePath))
			{
				System.IO.File.Delete(oldImagePath);
			}
			_unitOfWork.Product.Remove(productToBeDeleted);
			await _unitOfWork.SaveAsync();
			return Json(new { success = true, message = "Record has been deleted!" });
		}

		#endregion
	}
}
