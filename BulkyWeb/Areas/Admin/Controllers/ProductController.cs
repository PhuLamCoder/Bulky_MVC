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
				// Include the name same as in DbContext
				productVM.Product = _unitOfWork.Product.Get(product => product.Id == id, includeProperties: "ProductImages");
				return View(productVM);
			}
		}

		[HttpPost]
		public async Task<IActionResult> Upsert(ProductVM productVM, List<IFormFile>? files)
		{
			if (ModelState.IsValid)
			{
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

				string wwwRootPath = _webHostEnvironment.WebRootPath;
				if (files != null)
				{
					string productPath = "images/products/product-" + productVM.Product.Id;
					string finalPath = Path.Combine(wwwRootPath, productPath);

					if (!Directory.Exists(finalPath))
					{
						Directory.CreateDirectory(finalPath);
					}

					// Always null because no asp-for of it in the UpSert file
					if (productVM.Product.ProductImages == null)
					{
						productVM.Product.ProductImages = new List<ProductImage>();
					}

					foreach (IFormFile file in files)
					{
						string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
						using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
						{
							await file.CopyToAsync(fileStream);
						}

						ProductImage productImage = new()
						{
							ImageUrl = productPath + "/" + fileName,
							ProductId = productVM.Product.Id
						};
						
						// This list will automatically updated to the ProductImage table
						productVM.Product.ProductImages.Add(productImage);
					}
					_unitOfWork.Product.Update(productVM.Product);
					await _unitOfWork.SaveAsync();
				}

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

		public async Task<IActionResult> DeleteImage(int imageId)
		{
			var imageToBeDeleted = _unitOfWork.ProductImage.Get(img => img.Id == imageId);
			int productId = imageToBeDeleted.ProductId;
			if (imageToBeDeleted != null)
			{
				if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
				{
					var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl);
					if (System.IO.File.Exists(oldImagePath))
					{
						System.IO.File.Delete(oldImagePath);
					}
				}
				_unitOfWork.ProductImage.Remove(imageToBeDeleted);
				await _unitOfWork.SaveAsync();
				TempData["succes"] = "Deleted image successfully!!!";
			}
			return RedirectToAction(nameof(Upsert), new { id = productId });
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
			var productToBeDeleted = _unitOfWork.Product.Get(product => product.Id == id, includeProperties: "ProductImages");
			if (productToBeDeleted == null)
			{
				return Json(new { success = false, message = "Error while deleting!" });
			}

			string productPath = "images/products/product-" + id;
			string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

			if (Directory.Exists(finalPath))
			{
				Directory.Delete(finalPath, true);
			}

			_unitOfWork.Product.Remove(productToBeDeleted);
			await _unitOfWork.SaveAsync();
			return Json(new { success = true, message = "Record has been deleted!" });
		}

		#endregion
	}
}
