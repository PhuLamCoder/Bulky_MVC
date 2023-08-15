﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BulkyBook.Models
{
	public class ProductImage
	{
        public int Id { get; set; }

        [Required]
        public string? ImageUrl { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}
