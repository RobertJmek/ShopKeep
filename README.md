# ShopKeep

# Baza de date(ERD-initiala):

## Structura Bazei de Date

```mermaid
erDiagram
    ASPNETUSERS ||--o{ ORDERS : "plaseaza"
    CATEGORIES ||--o{ PRODUCTS : "contine"
    ORDERS ||--|{ ORDER_ITEMS : "include"
    PRODUCTS ||--o{ ORDER_ITEMS : "este_vandut_in"

    ASPNETUSERS {
        string Id PK
        string Email
        string FullName
        string Address
    }

    CATEGORIES {
        int Id PK
        string Name
    }

    PRODUCTS {
        int Id PK
        string Name
        decimal Price
        int CategoryId FK
    }

    ORDERS {
        int Id PK
        string UserId FK
        datetime OrderDate
        decimal TotalAmount
    }

    ORDER_ITEMS {
        int Id PK
        int OrderId FK
        int ProductId FK
        int Quantity
    }
```
