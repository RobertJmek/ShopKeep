# ShopKeep

# Baza de Date - Structura Actualizată

## Diagrama Entitate-Relație (ERD)

```mermaid
erDiagram
    ASPNETUSERS ||--o{ ORDERS : "plaseaza"
    ASPNETUSERS ||--o{ PRODUCTS : "propune"
    ASPNETUSERS ||--o{ REVIEWS : "scrie"
    ASPNETUSERS ||--o{ SHOPPING_CART_ITEMS : "are_in_cos"
    ASPNETUSERS ||--o{ WISHLIST_ITEMS : "doreste"
    
    CATEGORIES ||--o{ PRODUCTS : "contine"
    
    PRODUCTS ||--o{ REVIEWS : "are_review-uri"
    PRODUCTS ||--o{ SHOPPING_CART_ITEMS : "este_in_cos"
    PRODUCTS ||--o{ WISHLIST_ITEMS : "este_in_wishlist"
    PRODUCTS ||--o{ ORDER_ITEMS : "este_vandut_in"
    PRODUCTS ||--o{ PRODUCT_FAQS : "are_faqs"
    
    ORDERS ||--|{ ORDER_ITEMS : "include"

    ASPNETUSERS {
        string Id PK
        string UserName
        string Email
        string FullName
        datetime DateOfBirth
        string Address
        string PhoneNumber
        string AlternatePhoneNumber
    }

    CATEGORIES {
        int Id PK
        string Name UK "Unic, max 50 caractere"
    }

    PRODUCTS {
        int Id PK
        string Title "Required, max 100 caractere"
        string Description "Optional, max 2000 caractere"
        string ImageUrl "Optional, max 500 caractere"
        decimal Price "Required, > 0"
        int Stock "Required, >= 0"
        double AverageRating "Calculat automat 0-5"
        int Status "0=Pending, 1=Approved, 2=Rejected"
        string AdminFeedback "Optional, max 500 caractere"
        string ProposedByUserId FK "Nullable"
        int CategoryId FK "Required, Cascade Delete"
        datetime CreatedAt
        datetime UpdatedAt
    }

    REVIEWS {
        int Id PK
        int Rating "Optional 1-5"
        string Text "Optional max 1000 caractere"
        datetime CreatedAt
        datetime UpdatedAt
        string UserId FK "Required"
        int ProductId FK "Required"
    }

    SHOPPING_CART_ITEMS {
        int Id PK
        int Quantity "Min 1"
        datetime AddedAt
        string UserId FK "Required"
        int ProductId FK "Required"
    }

    WISHLIST_ITEMS {
        int Id PK
        datetime AddedAt
        string UserId FK "UK cu ProductId"
        int ProductId FK "UK cu UserId"
    }

    ORDERS {
        int Id PK
        string UserId FK "Required"
        datetime OrderDate
        decimal TotalAmount
        string Status "Plasată/În procesare/Livrată/Anulată"
        string DeliveryAddress "Required, max 500"
    }

    ORDER_ITEMS {
        int Id PK
        int OrderId FK "Required"
        int ProductId FK "Nullable - permite ștergere produs"
        string ProductTitle "Max 200 - info salvată"
        string ProductImageUrl "Max 500 - info salvată"
        int Quantity "Min 1"
        decimal UnitPrice "Preț salvat la comandă"
    }

    PRODUCT_FAQS {
        int Id PK
        int ProductId FK "Required"
        string Question "Required, max 500 caractere"
        string Answer "Required, max 2000 caractere"
        int AskCount "Număr întrebări similare"
        datetime CreatedAt
        datetime UpdatedAt
    }
```

## Descriere Tabele

### AspNetUsers (Identity Framework)
Tabelă gestionată de ASP.NET Core Identity pentru autentificare și autorizare.
- **Roluri**: Admin, Editor, User
- Super Admin: `admin@shopkeep.com` (protejat împotriva modificărilor)

### Categories
Categorii de produse pentru organizare și filtrare.
- **Constrangeri**: Nume unic, validare lungime 2-50 caractere
- **Cascade Delete**: Ștergerea categoriei șterge și produsele asociate

### Products
Produse propuse de utilizatori sau adăugate de admin/editori.
- **Status**:
  - `0 = Pending` - În așteptare aprobare
  - `1 = Approved` - Aprobat, vizibil pentru utilizatori
  - `2 = Rejected` - Respins de admin
- **Average Rating**: Calculat automat din reviews
- **Cascade**: Ștergerea produsului păstrează OrderItems (ProductId = null, info salvată)

### Reviews
Review-uri și rating-uri pentru produse.
- **Rating**: Opțional, 1-5 stele
- **Text**: Opțional, maxim 1000 caractere
- **Unicitate**: Un utilizator poate avea maxim un review per produs

### ShoppingCartItems
Coș de cumpărături pentru utilizatori autentificați.
- **Validare**: Cantitate minimă 1, verificare stoc disponibil
- **Subtotal**: Proprietate calculată (Price × Quantity)

### WishlistItems
Lista de dorințe (favorite) pentru utilizatori autentificați.
- **Constrangere**: Combinație unică UserId-ProductId

### Orders
Comenzi plasate de utilizatori.
- **Status**: "Plasată", "În procesare", "Livrată", "Anulată"
- **Anulare**: Doar pentru comenzi cu status "Plasată", restaurează stocul

### OrderItems
Produse incluse într-o comandă.
- **Info Salvată**: Title și ImageUrl păstrate chiar dacă produsul e șters
- **ProductId Nullable**: Permite ștergerea produsului fără a afecta istoricul comenzilor
- **UnitPrice**: Prețul salvat la momentul comenzii (nu se actualizează dacă prețul produsului se schimbă)

### ProductFAQs (Nou - Asistent AI)
FAQ-uri generate automat pentru produse bazat pe întrebările utilizatorilor.
- **AskCount**: Contor pentru popularitatea întrebării
- **Auto-generare**: Sistemul AI salvează întrebări frecvente cu răspunsuri
- **Similaritate**: Algoritm Jaccard pentru matching întrebări (prag 70%)

## Migrări Database

### Migrări Importante
1. `InitialMigration` - Structură inițială
2. `CompleteShopKeepModels` - Modele complete
3. `CascadeDeleteCategoryProducts` - Cascade delete pentru categorii
4. `MakeProductIdNullableInOrderItems` - ProductId nullable în OrderItems
5. `PreserveProductInfoInOrderItems` - Adăugare ProductTitle și ProductImageUrl

## Seed Data

### Utilizatori Inițiali
- **Admin**: `admin@shopkeep.com` / `Admin123!`
- **Editor**: `editor@shopkeep.com` / `Editor123!`
- **User**: `user@shopkeep.com` / `User123!`

### Date Inițiale
- 5 categorii predefinite
- 10 produse de test
- Review-uri și rating-uri pentru demonstrație
