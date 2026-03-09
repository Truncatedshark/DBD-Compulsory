# Migration Log

---

## V001 — Initial Schema

Creates the initial `Books`, `Members`, and `Loans` tables and exposes the four baseline API endpoints.

**Type of change:** Additive (non-breaking) — fresh schema, no existing clients.

**API impact:** Introduced the four initial endpoints:
- `GET /api/books`
- `GET /api/members`
- `POST /api/loans`
- `GET /api/loans/{memberId}`

No existing behaviour was changed. This is the baseline the API contract is built on.

**Deployment notes:** First migration. No prior state exists. Safe to apply at any time before the application starts.

**Decisions and tradeoffs:** Foreign keys on `Loans` use `ON DELETE RESTRICT` to prevent orphaned loan records if a book or member is deleted. Email was indexed as unique from the start to reflect its role as an identifier for members.

---

## V002 — Add Authors

Adds an `Authors` table and an `AuthorBook` join table, and includes author information in the `GET /api/books` response.

**Type of change:** Additive (non-breaking) — new tables added, no existing columns removed or modified.

**API impact:** `GET /api/books` now includes an `authors` array in the response:
```json
{
  "id": 1,
  "title": "The Pragmatic Programmer",
  "isbn": "978-0135957059",
  "publicationYear": 1999,
  "authors": [
    { "firstName": "Unknown", "lastName": "Author", "biography": null }
  ]
}
```
Adding a new field to an existing response is generally considered non-breaking — existing clients that do not read `authors` are unaffected. No versioning was introduced.

**Deployment notes:** Migration must be applied before the new application code is deployed. During the window between migration and redeployment, the old code will query `Books` without including `Authors` — this is safe since no columns were removed. The new code uses EF Core's query projection and does not rely on eager loading, so it will not fail on books with no authors beyond the placeholder.

**Decisions and tradeoffs:** Existing books had no authors after the migration. Rather than leaving them in an authorless state (which would return `"authors": []` for every existing book), I added an "unknown author" manually with SQL to ensure an enforce that we dont get authorless books in code while also supporting books that actually have no known authors. The join table `AuthorBook` is managed implicitly by EF Core since the relationship carries no extra data. The `AuthorBook` inserts use a subquery to find the placeholder by name rather than hardcoding an ID, making the data migration robust against identity sequence behaviour.

---

## V003 — Unique Email Constraint + Phone Number

Adds a unique constraint on `Members.Email`, renames duplicate emails to satisfy it, and adds a mandatory `PhoneNumber` column with a temporary default.

**Type of change:** Additive (potentially breaking) — new `NOT NULL` column added to `Members`; duplicate email data modified before unique constraint enforced.

**API impact:** `GET /api/members` now includes `phoneNumber` in the response. Adding a new field is non-breaking for existing clients. No versioning introduced. The email uniqueness constraint has no direct API impact — it is enforced at the database level and affects write operations only.

**Deployment notes:** Migration must be applied before the new application code is deployed. During the window between migration and redeployment, the old code will attempt inserts without `PhoneNumber` — these will succeed because the column has a database-level `DEFAULT 'UNKNOWN'`. Once the new code is deployed, it will be responsible for collecting real phone numbers from users.

**Decisions and tradeoffs:**
In this case i chose to rename duplicate emails sequentially so that i could implement the new Unique restraint without breaking the migration, this allows us to retain the original e-mail now with an appended addition to its name. Solving the actual conflict/problem would then be operational, prompting the user to reset their email, log in once with their new temporary email. This protects user data from being deleted, while not being a perfect solution this is what ive come up with.

I went with a similar approach for the PhoneNumber, adding it as required and using HasDefaultValue("unknown") to make the migration work and fill-in the already existing member tables with the default unknown phonenumber, with the expectation that those users can later add their phonenumbers or be prompted to do so. This is not optimal but once all three users have updated their phonenumbers, a future migration would enable it to be reversed back to not using the unknown default as this brings uncertainty into whether or not phonenumbers are actually saved in the Database correctly.

---

## V004 — Loan Status

Adds a `Status` column to `Loans` with four possible values, backfilling existing rows from `ReturnDate`, while keeping `ReturnDate` intact for existing clients.

**Type of change:** Additive (non-breaking) — new `NOT NULL` column added to `Loans`; existing data backfilled from `ReturnDate`.

**API impact:** `GET /api/loans/{memberId}` now includes a `status` field in the response. Adding a new field is non-breaking — the frontend team reading `returnDate` is unaffected. No versioning introduced; the existing endpoint was modified in place.

**Deployment notes:** Migration must be applied before the new application code is deployed. During the window between migration and redeployment, the old code creates loans without setting `Status` explicitly — these default to `"Active"` via the C# enum default. Once the new code deploys, `Status = LoanStatus.Active` is set explicitly on loan creation.

**Decisions and tradeoffs:**
It doesnt say anywhere that ReturnDate will be made obsolete, so therefore in an additive fashion i created the Status enum inside the loan.cs and added it to my DbContext, so that the frontend developers can continue using ReturnDate until they are ready to push their new version which supports the Status enum. Regarding the current loans in the database i added two lines of SQL to the sql artifact that sets all current entries in the Loans table's status to either active or returned based on whether or not they have a return date, then in the future when the frontend developers push their new version they can support the other statuses as well. If they just read the API outputs ReturnDate then they can continue to do that as long as they want.

As i cannot derive whether or not a book is lost or overdue with the data presented i only chose to populate based on what data i have which is Active or Returned based on the ReturnDate.

I chose to save the status as String instead of Int due to readability even though its a less rigid datatype.

---

## V005 — Book Retirement (Soft Delete)

Adds an `IsRetired` flag to `Books`, filters retired books from the catalogue endpoint, and prevents new loans being created against retired books.

**Type of change:** Additive (non-breaking) — new boolean column added to `Books` with `DEFAULT FALSE`. No existing columns removed or modified.

**API impact:**
- `GET /api/books` now excludes retired books from results. The response shape is unchanged — existing clients are unaffected as long as they do not depend on a specific book remaining in the catalogue.
- `POST /api/loans` now returns `422 Unprocessable Entity` if the requested book is retired. This is a new failure mode on an existing endpoint.
- `GET /api/loans/{memberId}` is unchanged. Retired book details remain visible in loan history for auditing purposes.

**Deployment notes:** Migration must be applied before the new code is deployed. During the window between migration and redeployment, the old code queries `Books` without the `IsRetired` filter — retired books appear in results and loans against them can still be created. Since no books are retired at deploy time, this window is safe in practice.

**Decisions and tradeoffs:**
Code Review Response:
"This could work but doesnt take into account that we need to keep retired book entries for auditing, adding this on an EF core global query filter would apply this to all instances of fetching books including loan responses, therefore i moved your IsDeleted filtering.  I renamed the IsDeleted field to IsRetired as it isnt being used to strictly delete these entries and instead added it to our BooksController so that it is responsible for filtering for itself while leaving loans untouched. "

I also needed to handle creating new loans with retired books and made it a failure mode.
Keeping the filtering for retired books in the controller enabled me to keep the filtering isolated and local to only the Bookscontroller.
---

## V006 — Fix ISBN Column Type

Replaces the corrupt integer `Isbn` column with a nullable text column, nulls out unrecoverable data, and introduces `GET /api/v2/books` as a versioned replacement endpoint.

**Type of change:**  Destructive — existing column replaced and existing data invalidated. Requires coordination with API consumers to migrate from v1 to v2.

**API impact:**
- `GET /api/books` (v1) — `isbn` field now returns `null` for all existing books. The field remains in the response so existing clients do not break on the shape, but clients that depended on a numeric value will receive `null` instead. This endpoint is now considered deprecated.
- `GET /api/v2/books` — new endpoint introduced with the same response shape. Once correct ISBNs are re-entered, this endpoint will return them as proper strings. Clients should migrate to v2.
- No other endpoints are affected.

**Deployment notes:** Migration must be applied before the new code is deployed. The column replacement is performed in a single transaction — the old column is dropped and the new one renamed atomically. During the window between migration and redeployment, the old code will attempt to read `Isbn` which now returns `null` — this may cause unexpected behaviour in the old application if it does not handle null ISBNs. The deployment window should be kept as short as possible.

**Decisions and tradeoffs:**

For this section i chose to make a V2 of the books API endpoint to get the best of both worlds, a big messy change like this would completely change the output and break all of our poor frontend devs code. By making a V2 to prepare for when the frontenders catch up, we can delete the lost/corrupted data from the Database by using a 6-step column replacement i directly inserted into the SQL artifact: add new text column → null out corrupt data → drop the old index → drop old column → rename new column → recreate index

This makes it so that V1 just returns ISBN as null without failing/crashing. Once the frontend team is ready to migrate to V2 of the endpooint, it makes sense to keep them separate for changes like this where you know you are gonna break alot of things downstream.

