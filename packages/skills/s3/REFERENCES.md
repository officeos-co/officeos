# S3 Skill — References

## AWS S3 REST API
- **Docs**: https://docs.aws.amazon.com/AmazonS3/latest/API/Welcome.html
- **S3 REST API Reference**: https://docs.aws.amazon.com/AmazonS3/latest/API/API_Operations_Amazon_Simple_Storage_Service.html

## Auth: AWS Signature Version 4
- **Signing Guide**: https://docs.aws.amazon.com/general/latest/gr/signature-version-4.html
- Requests are signed with `access_key_id` + `secret_access_key` using HMAC-SHA256.
- The implementation derives signing key per-request: `HMAC(HMAC(HMAC(HMAC("AWS4" + secret, date), region), service), "aws4_request")`.

## Endpoint Pattern
```
https://s3.{region}.amazonaws.com/{bucket}/{key}
```
or path-style (us-east-1 default): `https://s3.amazonaws.com/{bucket}/{key}`

## Operations Covered
| Operation | HTTP Method | Notes |
|-----------|-------------|-------|
| ListBuckets | GET / | Lists all buckets |
| CreateBucket | PUT /{bucket} | Creates a new bucket |
| DeleteBucket | DELETE /{bucket} | Deletes an empty bucket |
| HeadBucket | HEAD /{bucket} | Checks if bucket exists |
| ListObjectsV2 | GET /{bucket}?list-type=2 | Lists objects with pagination |
| GetObject | GET /{bucket}/{key} | Returns object body |
| PutObject | PUT /{bucket}/{key} | Uploads object |
| DeleteObject | DELETE /{bucket}/{key} | Deletes an object |
| DeleteObjects | POST /{bucket}?delete | Batch delete |
| HeadObject | HEAD /{bucket}/{key} | Object metadata |
| CopyObject | PUT /{bucket}/{key} x-amz-copy-source | Server-side copy |
| GetObjectPresigned | Pre-signed GET URL | Time-limited download URL |
| CreateMultipartUpload | POST /{bucket}/{key}?uploads | Initiates multipart upload |
| CompleteMultipartUpload | POST /{bucket}/{key}?uploadId=ID | Finalizes multipart upload |
| AbortMultipartUpload | DELETE /{bucket}/{key}?uploadId=ID | Cancels multipart upload |

## License
AWS SDK for JavaScript v3 (Apache-2.0): https://github.com/aws/aws-sdk-js-v3
