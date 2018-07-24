# PreviewImageHandler

一行代码即可生成缩略图的HttpHandler，支持裁剪、留白等不同模式

## 功能特性

* 一个.ashx即可提供生成缩略图并进行缓存的功能

* 仅仅一个模式、三个必备参数，易于使用

* 缓存缩略图，高性能

* 基于异步MicroAPI，高并发

* 可配置缩略图文件存储位置和图片压缩质量

* 使用GDI+进行缩放，支持.bmp, .gif, .jpg, .jpeg, .png, .tiff, .exif

## 使用方法

PreviewImageHandler使用起来非常简单，就一个PreImg.ashx，支持三种模式和三个必备参数

你只需要将PreImg.ashx、PreImg.ashx.cs、MicroAPIAsync.cs复制到自己的项目中即可

浏览Default.aspx即可查看示例，有意见建议请提交issue，非正式交流请加QQ群：163203102

### 缩放模式

模式|说明|示例
-|-|-
d|(Default)默认模式，非等比缩放，不裁切，不留白，会变形|PreImg.ashx/**d**?url=win7.jpg&w=200&h=200
c|(Cut)裁切模式，等比缩放，损失图片内容，不变形，不留白|PreImg.ashx/**c**?url=win7.jpg&w=200&h=200
lw|(LeaveWhite)留白模式，等比缩放,不变形，有留白背景色|PreImg.ashx/**lw**?url=win7.jpg&w=200&h=200

### 必备参数

#### url

要缩放的图片路径，为避免包含符号导致的转义问题，强烈推荐对该参数值进行url编码

### w

缩放宽度，大于0的int

### h

缩放高度，大于0的int

## 扩展配置

### PreImg.ThumbnailLocation

缩略图保存位置，默认为`"~/PreImgTemp/"`

### PreImg.Quality

jpg压缩质量，0-100，默认为`75`

### PreImg.LeaveWhiteColor

留白背景色，默认为`Color.White`

## 最佳实践

* 为了避免url转义问题，强烈推荐对url参数进行url编码

* 除了通过网址调用之外，本组件开放了核心缩放方法`GenerateThumbnail`，你可以根据参数自行调用

* 开源不宜，长期坚持更难，如果对你有用，请不要吝啬你的关注、加星、建议、推广、贡献、捐赠

## 下一步

* 收集bug问题和修改意见，修复完善

* 增加英文文档

* 研究移植到ASP.NET Core的可能性