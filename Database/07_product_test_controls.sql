-- Chỉ thao tác trên hệ thống sản phẩm giả lập bên ngoài; không phải chức năng e-SHOPPING.
USE EShopping_ProductMock;
GO
-- Chạy riêng từng UPDATE để mô phỏng thay đổi sau khi đưa sản phẩm vào giỏ.
-- UPDATE dbo.Product SET Price=600000 WHERE Id='SP001';
-- UPDATE dbo.Product SET InStock=0 WHERE Id='SP001';
-- Khôi phục dữ liệu mẫu sau kiểm thử:
-- UPDATE dbo.Product SET Price=500000,InStock=1 WHERE Id='SP001';
SELECT Id,Name,Price,InStock FROM dbo.Product;
GO
