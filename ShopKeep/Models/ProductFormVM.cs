using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ShopKeep.Models;

public class ProductFormVM
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Titlul produsului este obligatoriu")]
    [StringLength(200, ErrorMessage = "Titlul este prea lung. Maxim 200 de caractere.")]
    [Display(Name = "Titlu")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descrierea este obligatorie")]
    [StringLength(2000, ErrorMessage = "Descrierea este prea lungă. Maxim 2000 de caractere.")]
    [Display(Name = "Descriere")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prețul este obligatoriu")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie mai mare decât 0")]
    [Display(Name = "Preț")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stocul este obligatoriu")]
    [Range(0, int.MaxValue, ErrorMessage = "Stocul trebuie să fie mai mare sau egal cu 0")]
    [Display(Name = "Stoc")]
    public int Stock { get; set; }

    [Required(ErrorMessage = "Categoria este obligatorie")]
    [Display(Name = "Categorie")]
    public int CategoryId { get; set; }

    // New upload (optional on edit, required on create via controller validation)
    [Display(Name = "Imagine")]
    public IFormFile? ImageFile { get; set; }

    // Current image for edit
    public string? ExistingImageUrl { get; set; }
}
