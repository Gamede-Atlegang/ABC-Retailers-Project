
ABC Retailers - Azure Storage Integration

📋 Project Summary
A comprehensive retail management web application demonstrating full integration with Azure Storage services. The application provides a complete solution for managing customers, products, orders, and file uploads using Azure Tables, Blob Storage, Queues, and File Shares.

☁️ Azure Storage Services Implemented

1. Azure Tables
Customers Table: Customer profiles (name, email, address, username)
Products Table: Product information (name, description, price, stock, image URL)
Orders Table: Order details (customer, product, quantity, pricing, status)

2. Azure Blob Storage
product-images Container: Public access for product images
payment-proofs Container: Private access for sensitive documents

3. Azure Queues
order-notifications Queue: Order creation and processing messages
stock-updates Queue: Inventory management notifications

4. Azure File Shares
contracts Share: Organized storage for contracts and payment proofs

�� Core Features

👥 Customer Management
Complete CRUD operations for customer profiles
Form validation and error handling
Azure Table Storage integration

�� Product Management
Product CRUD with image upload to Azure Blob Storage
Real-time stock level tracking with visual indicators
Price management with proper decimal handling
Image preservation during edits

�� Order Management
Order creation with customer/product dropdowns
Real-time price calculation via AJAX
Stock validation and automatic updates
Order status management (Pending, Processing, Completed, Cancelled)
Queue notifications for order events

�� File Upload System
Payment proof uploads to Azure File Share
File type validation and size restrictions
Organized storage in payments directory
Queue notifications for upload events

📊 Dashboard
Real-time statistics (customer count, product count, order count)
Featured products display with images
Quick action buttons for common tasks

🏗️ Technical Architecture

🎮 Controllers
HomeController: Dashboard and error handling
CustomerController: Customer CRUD operations
ProductController: Product management with image uploads
OrderController: Order processing with AJAX pricing
UploadController: File upload to Azure File Share

�� Models
Customer: ITableEntity implementation
Product: ITableEntity with image URL support
Order: ITableEntity with pricing and status
FileUploadModel: File upload form model
HomeViewModel: Dashboard statistics
ErrorViewModel: Error handling

�� Services
IAzureStorageService: Interface defining all Azure operations
AzureStorageService: Complete implementation with:
Table operations (CRUD with ETag support)
Blob upload with content type preservation
Queue messaging
File Share operations with proper API usage

⚙️ Setup Instructions

📋 Prerequisites
Visual Studio 2022
Azure account with Storage Account
.NET 8.0 SDK

☁️ Azure Setup
Create Azure Storage Account
Get connection string from Access Keys
Update appsettings.json with connection string

�� Local Development
Clone repository
Open solution in Visual Studio
Update connection string in appsettings.json
Build and run application
Azure resources auto-created on first run

🚀 Deployment
Create Azure App Service
Configure connection string in App Service settings
Deploy from Visual Studio or GitHub
Verify all Azure Storage resources accessible

🔧 Key Technical Solutions

�� Data Type Handling
Used double for prices (Azure Tables doesn't support decimal)
Proper currency formatting with ToString("C2")
Numeric inputs with step validation

🔄 Concurrency Control
ETag support for optimistic concurrency
Fallback to ETag.All for missing ETags
Proper error handling for concurrency conflicts

📁 File Management
Image uploads to Blob Storage with public access
Payment proofs to File Share with organized directories
Content type preservation for uploaded files

⚡ Real-time Features
AJAX price calculation in order creation
Dynamic stock validation
Queue-based notifications

📈 Scalability & Reliability Features

📈 Scalability
Stateless Design: Horizontal scaling capability
Azure Tables: Partitioned storage for high performance
Blob Storage: CDN-ready for global image delivery
Queues: Decoupled processing for high throughput

🛡️ Reliability
Connection String Management: Secure configuration
Error Handling: Comprehensive exception management
ETag Support: Optimistic concurrency for data integrity
Auto-Resource Creation: Ensures required Azure resources exist

�� Cost Effectiveness
Standard Storage: Balanced performance and cost
Locally Redundant Storage: Cost-effective redundancy
Efficient Queries: Optimized table queries
Proper Resource Management: No unnecessary storage usage

🧪 Testing Scenarios

✅ Basic Functionality
Add customer → Verify Azure Tables
Add product with image → Verify Blob Storage
Create order → Verify stock updates and queue messages
Upload payment proof → Verify File Share

�� Advanced Features
Concurrency: Multiple users editing same record
Error Handling: Invalid file uploads, network issues
Performance: Large number of records
Security: Connection string protection

📸 Screenshots Checklist for POE Submission

☁️ Azure Portal Screenshots
Storage Account Overview
Tables Section (Customers, Products, Orders)
Blob Containers (product-images, payment-proofs)
Queues (order-notifications, stock-updates)
File Shares (contracts with payments directory)

🖥️ Application Screenshots
Home Dashboard (statistics and featured products)
Customer List (CRUD operations)
Product Management (images and stock badges)
Order Creation (dropdowns and AJAX pricing)
File Upload (payment proof interface)
Order Details (complete information display)

📁 File Naming
Format: StudentNumber_ModuleCode_Part1.docx
Example: 12345_CLDV6212_Part1.docx

🔧 Troubleshooting

❗ Common Issues
Connection String Error: Verify Azure Storage connection string
Resource Creation Failed: Check Azure permissions
Image Upload Issues: Verify blob container permissions
Queue Messages: Check queue client configuration

�� Debug Information
Enable detailed logging in appsettings.json
Check Azure Storage metrics in portal
Verify resource creation in Azure portal

🔮 Future Enhancements
User authentication and authorization
Advanced reporting and analytics
Integration with payment gateways
Mobile application support
Advanced search and filtering
Bulk import/export functionality

🎯 Project Achievement
This project demonstrates complete integration of all four Azure Storage services (Tables, Blobs, Queues, File Shares) in a production-ready retail management system with proper error handling, concurrency control, and scalability considerations.

