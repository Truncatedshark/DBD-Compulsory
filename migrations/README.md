# Migration Log

---

## V001 — Initial Schema

**Type of change:** Additive (non-breaking) — fresh schema, no existing clients.

**API impact:** Introduced the four initial endpoints:
- `GET /api/books`
- `GET /api/members`
- `POST /api/loans`
- `GET /api/loans/{memberId}`

No existing behaviour was changed. This is the baseline the API contract is built on.

**Deployment notes:** First migration. No prior state exists. Safe to apply at any time before the application starts.

**Decisions and tradeoffs:** Foreign keys on `Loans` use `ON DELETE RESTRICT` to prevent orphaned loan records if a book or member is deleted. Email was indexed as unique from the start to reflect its role as a natural identifier for members.

---

## V002 — Add Authors

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

**Type of change:** Additive (potentially breaking) — new `NOT NULL` column added to `Members`; duplicate email data modified before unique constraint enforced.

**API impact:** `GET /api/members` now includes `phoneNumber` in the response. Adding a new field is non-breaking for existing clients. No versioning introduced. The email uniqueness constraint has no direct API impact — it is enforced at the database level and affects write operations only.

**Deployment notes:** Migration must be applied before the new application code is deployed. During the window between migration and redeployment, the old code will attempt inserts without `PhoneNumber` — these will succeed because the column has a database-level `DEFAULT 'UNKNOWN'`. Once the new code is deployed, it will be responsible for collecting real phone numbers from users.

**Decisions and tradeoffs:**
In this case i chose to rename duplicate emails sequentially so that i could implement the new Unique restraint without breaking the migration, this allows us to retain the original e-mail now with an appended addition to its name. Solving the actual conflict/problem would then be operational, prompting the user to reset their email, log in once with their new temporary email. This protects user data from being deleted, while not being a perfect solution this is what ive come up with.

I went with a similar approach for the PhoneNumber, adding it as required and using HasDefaultValue("unknown") to make the migration work and fill-in the already existing member tables with the default unknown phonenumber, with the expectation that those users can later add their phonenumbers or be prompted to do so. This is not optimal but once all three users have updated their phonenumbers, a future migration would enable it to be reversed back to not using the unknown default as this brings uncertainty into whether or not phonenumbers are actually saved in the Database correctly.

---