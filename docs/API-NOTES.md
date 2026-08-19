# API notes

This SDK prefers official OpenAPI specifications from https://github.com/Stedi/openApi as the wire-format source of truth. Endpoint documentation at https://www.stedi.com/docs/healthcare/api-reference is used for behavior that is documented but incomplete in OpenAPI.

## Pagination field names

- Healthcare docs describe `next_page_token` in JSON and `page_token` as a query parameter.
- Current OpenAPI specs use camelCase `nextPageToken` and `pageToken`.
- This SDK sends and reads the OpenAPI camelCase names.

## Payer hosts

- `healthcare.json` duplicates payer list/search/get on `https://healthcare.us.stedi.com/2024-04-01`.
- `payers.json` is the dedicated Payers API on `https://payers.us.stedi.com/2024-04-01`.
- The SDK uses the dedicated Payers API.

## Raw X12

- Docs describe raw X12 endpoints as EDI payloads.
- OpenAPI request bodies are JSON objects with a required `x12` string, for example `{ "x12": "ISA*..." }`.
- Public methods accept `string x12` and wrap that JSON object. Content-Type is `application/json`.

## SOAP eligibility

- `healthcare.json` contains `POST /eligibility-check` with no request or response schema.
- Current docs specify `POST https://healthcare.us.stedi.com/2025-06-01/protocols/caqh-core` with `Content-Type: application/soap+xml`.
- Authentication for SOAP is WS-Security in the envelope, not the `Authorization` header.
- The SDK implements the documented SOAP URL and content type.

## Enrollment and claim-attachment uploads

- These APIs return a pre-signed URL. Callers `PUT` the file to that URL.
- They are not `multipart/form-data` in the current OpenAPI specs.
- The SDK uploads from a `Stream` via HTTP `PUT` and does not attach the Stedi API key to the pre-signed host.

## CMS-1500 PDFs

- By business identifier (`GET /export/pdf`): JSON containing base64 PDF strings.
- By transaction ID (`GET /export/{transactionId}/1500/pdf`): OpenAPI types the 200 body as a string / `application/octet-stream`. Docs say the body is a base64 string. The SDK decodes base64 and also accepts a raw `%PDF` body.

## 835 ERA PDF

- Requires `Accept: application/pdf`. The SDK sends that header and returns a streamed `StediFileResponse`.

## Eligibility PDF

- Default documented response can be base64 JSON; sending `Accept: application/pdf` returns binary PDF. The SDK requests `application/pdf`.

## Healthcare API version

- `healthcare.json` lists both `https://healthcare.us.stedi.com/2024-04-01` and `https://healthcare.us.stedi.com/2026-06-01`.
- The SDK defaults to `2024-04-01`, matching current endpoint documentation examples. Override `StediHealthcareOptions.HealthcareBaseUrl` if you need the newer root.
