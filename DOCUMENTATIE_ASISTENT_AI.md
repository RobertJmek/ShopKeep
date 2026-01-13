# Documentație - Asistent AI pentru Produse

## Prezentare Generală

Asistentul AI este o funcționalitate integrată în aplicația ShopKeep care permite utilizatorilor să primească răspunsuri instantanee la întrebări despre produse, fără a fi nevoie să parcurgă întreaga descriere sau să contacteze vânzătorul.

## Arhitectură și Funcționalitate

### 1. Componente Principale

#### 1.1 Frontend - Chat Widget (`Views/Product/Show.cshtml`)

**Widget Flotant**
- Buton fix în colțul dreapta-jos al paginii produsului
- Design modern cu animații smooth (slideUp, fadeIn)
- Interfață chat intuitivă cu mesaje diferențiate pentru utilizator (albastru) și AI (gri)
- Responsive - se adaptează pe mobile (lățime ajustată, butoane mai mici)

**Caracteristici UI:**
- Header cu iconița robotului și titlul "Asistent AI"
- Zonă de mesaje scrollable (400px înălțime)
- Input pentru întrebări cu buton de trimitere
- Badge-uri pentru sursa răspunsului: "Din FAQ", "Din descriere", "Răspuns general"
- Indicatori de încredere (confidence): high (verde), medium (albastru), low (gri)
- Loading indicator animat în timpul procesării

#### 1.2 Backend - API Controller (`Controllers/ProductAIChatController.cs`)

**Endpoint Principal: `POST /api/ProductAIChat/ask`**

```csharp
Request Body:
{
    "productId": int,
    "question": string
}

Response:
{
    "answer": string,
    "source": "faq" | "description" | "default",
    "confidence": "high" | "medium" | "low"
}
```

#### 1.3 Model de Date (`Models/ProductFAQ.cs`)

**Structura FAQ-urilor:**
- `Id` - identificator unic
- `ProductId` - referință către produs
- `Question` - întrebarea frecventă (max 500 caractere)
- `Answer` - răspunsul la întrebare (max 2000 caractere)
- `AskCount` - contor pentru popularitate (câte ori a fost pusă întrebarea)
- `CreatedAt` / `UpdatedAt` - timestamp-uri pentru tracking

### 2. Procesul de Răspundere

#### Pasul 1: Primirea Întrebării
1. Utilizatorul tastează o întrebare în chat
2. Frontend trimite request POST către API cu `productId` și `question`
3. Backend validează întrebarea (nu poate fi goală) și verifică existența produsului

#### Pasul 2: Căutare în FAQ-uri Existente
```csharp
// Algoritm de căutare cu similaritate Jaccard
foreach (var faq in existingFaq)
{
    var similarity = CalculateSimilarity(normalizedQuestion, normalizedFaqQuestion);
    if (similarity > 0.7) // 70% similaritate
    {
        faq.AskCount++; // Incrementează popularitatea
        return faq.Answer; // Răspuns cu confidence HIGH
    }
}
```

**Normalizare Text:**
- Conversie la lowercase
- Eliminare diacritice (ă→a, â→a, î→i, ș→s, ț→t)
- Eliminare punctuație și caractere speciale
- Eliminare spații multiple

**Calcul Similaritate:**
- Algoritm: **Jaccard Similarity**
- Formula: `intersection(words1, words2) / union(words1, words2)`
- Prag acceptare: **70%**

#### Pasul 3: Generare Răspuns din Descrierea Produsului

Dacă nu există FAQ similar, sistemul analizează întrebarea și generează răspuns:

**3.1 Întrebări despre Preț**
```csharp
Cuvinte cheie: "pret", "costa", "cost", "bani", "lei", "valoare"
Răspuns: "Prețul produsului este {price} lei."
Confidence: MEDIUM
```

**3.2 Întrebări despre Stoc**
```csharp
Cuvinte cheie: "stoc", "disponibil", "stock"
Răspuns: "Da, produsul este în stoc. Avem {stock} bucăți disponibile."
         sau "Din păcate, produsul nu este momentan în stoc."
Confidence: MEDIUM
```

**3.3 Căutare în Descriere**
```csharp
// Extrage cuvinte semnificative din întrebare (lungime > 3)
// Împarte descrierea în propoziții
// Numără match-uri pentru fiecare propoziție
// Returnează cele mai relevante 2 propoziții
Confidence: MEDIUM
```

**3.4 Răspuns Default**
```csharp
"Nu am suficiente informații în descrierea produsului pentru a răspunde 
la această întrebare. Vă rugăm să contactați vânzătorul pentru mai multe detalii."
Confidence: LOW
```

#### Pasul 4: Salvare Automată FAQ
După generarea unui răspuns, sistemul verifică dacă întrebarea merită salvată:
- Se compară cu FAQ-uri existente (similaritate > 70%)
- Dacă e unică, se creează un nou `ProductFAQ` cu `AskCount = 1`
- La următoarea întrebare similară, `AskCount` se incrementează
- FAQ-urile populare devin surse permanente de răspunsuri

### 3. Fluxul Complet de Interacțiune

```
┌─────────────┐
│  Utilizator │
└──────┬──────┘
       │
       │ 1. Pune întrebare în chat
       ▼
┌──────────────────┐
│  Chat Widget     │
│  (Frontend JS)   │
└──────┬───────────┘
       │
       │ 2. POST /api/ProductAIChat/ask
       ▼
┌──────────────────────────┐
│  ProductAIChatController │
│  (Backend API)           │
└──────┬───────────────────┘
       │
       │ 3. Caută în FAQ-uri (DB)
       ▼
┌──────────────┐     ┌─────────────────┐
│  ProductFAQ  │────▶│ Similaritate    │
│  (Database)  │     │ > 70% ?         │
└──────────────┘     └────┬─────┬──────┘
                          │     │
                   DA │     │ NU
                      ▼     ▼
              ┌─────────────────────┐
              │ Returnează FAQ      │
              │ confidence: HIGH    │
              └──────────┬──────────┘
                         │
              ┌──────────▼──────────────┐
              │ Generează din descriere │
              │ confidence: MEDIUM/LOW  │
              └──────────┬──────────────┘
                         │
              ┌──────────▼──────────┐
              │ Salvează FAQ nou    │
              │ AskCount = 1        │
              └──────────┬──────────┘
                         │
                         │ 4. Răspuns JSON
                         ▼
              ┌──────────────────┐
              │  Chat Widget     │
              │  Afișează răspuns│
              └──────────────────┘
```

## Avantaje ale Implementării

### 1. **Învățare Automată**
- Sistemul învață din întrebările utilizatorilor
- FAQ-urile populate cresc calitatea răspunsurilor
- Contor `AskCount` identifică întrebările frecvente

### 2. **Performanță**
- Răspunsuri instantanee (< 1 secundă)
- Căutare optimizată în baza de date
- Algoritmi simpli dar eficienți (Jaccard similarity)

### 3. **Experiență Utilizator**
- Nu necesită autentificare
- Interfață intuitivă și familiară (chat)
- Răspunsuri contextualizate la produs
- Vizibilitate asupra sursei informației

### 4. **Reducerea Load-ului pentru Vânzători**
- Întrebări repetitive rezolvate automat
- Clienții primesc răspunsuri imediate
- Vânzătorii pot vedea care sunt cele mai frecvente întrebări

## Limitări și Îmbunătățiri Viitoare

### Limitări Actuale
- Nu folosește un model de NLP avansat (GPT, BERT)
- Răspunsurile sunt limitate la conținutul descrierii produsului
- Nu poate răspunde la întrebări complexe sau comparative
- Nu suportă conversații multi-turn (fiecare întrebare e independentă)

### Îmbunătățiri Propuse
1. **Integrare OpenAI API**
   - Răspunsuri mai naturale și contextuale
   - Generare răspunsuri pentru informații lipsă

2. **Context Conversațional**
   - Memorarea conversației precedente
   - Răspunsuri bazate pe istoricul întrebărilor

3. **Analitică Avansată**
   - Dashboard pentru vânzători cu întrebări frecvente
   - Sugestii automate pentru îmbunătățirea descrierilor
   - Identificare gap-uri de informații

4. **Multi-limbaj**
   - Detectare automată a limbii
   - Suport pentru română și engleză

5. **Răspunsuri Rich-Media**
   - Incluziune imagini din galerie
   - Link-uri către secțiuni relevante
   - Tabele comparative

## Tehnologii Utilizate

- **Frontend**: JavaScript (Vanilla), HTML5, CSS3, Bootstrap 5, Bootstrap Icons
- **Backend**: ASP.NET Core 9.0, C#
- **Database**: MySQL 8.0.31, Entity Framework Core 9.0
- **Pattern**: RESTful API
- **Algoritmi**: Jaccard Similarity pentru matching text

## Instrucțiuni de Testare

1. **Accesează o pagină de produs**
   ```
   URL: /Product/Show/{id}
   ```

2. **Click pe butonul flotant de chat** (colț dreapta-jos)

3. **Testează diferite tipuri de întrebări:**
   - "Cât costă?" → Răspuns despre preț
   - "Este în stoc?" → Răspuns despre disponibilitate
   - "Ce dimensiuni are?" → Căutare în descriere
   - "Ce material?" → Căutare în descriere

4. **Observă badge-urile de sursa și încredere:**
   - Verde (HIGH) = din FAQ existent
   - Albastru (MEDIUM) = generat din descriere
   - Gri (LOW) = răspuns default

5. **Testează aceeași întrebare de 2 ori:**
   - Prima dată: MEDIUM (din descriere)
   - A doua oară: HIGH (din FAQ salvat automat)

## Concluzie

Asistentul AI este o funcționalitate inovatoare care îmbunătățește semnificativ experiența utilizatorului pe platforma ShopKeep, oferind răspunsuri rapide și relevante la întrebări despre produse. Sistemul se îmbunătățește automat în timp, învățând din interacțiunile utilizatorilor și construind o bază de cunoștințe pentru fiecare produs.
