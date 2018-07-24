<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PreviewImageHandler.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>PreviewImageHandler</title>
    <style>
        h1 {
            color: #7070a0;
            font-size: 150%;
            margin: 0 0 1em 0;
        }

        h2 {
            color: #7070a0;
            font-size: 120%;
            margin: 2em 0 0 0;
        }

        pre {
            border: solid 1px #BBBBDD;
            background-color: #F0FFF0;
            padding: 0.5em;
        }
        div{
            background-color:#CCC;
            padding:10px;
        }
    </style>
</head>
<body>
    <h1>PreviewImageHandler</h1>
    <p>使用本组件非常简单，只需要设置正确的参数即可</p>
    <pre>&lt;img src="PreImg.ashx/d?url=win7.jpg&w=200&h=200" /&gt;</pre>
    <div><img src="PreImg.ashx/d?url=win7.jpg&w=200&h=200" /></div>
    <pre>&lt;img src="PreImg.ashx/c?url=win7.jpg&w=200&h=200" /&gt;</pre>
    <div><img src="PreImg.ashx/c?url=win7.jpg&w=200&h=200" /></div>
    <pre>&lt;img src="PreImg.ashx/lw?url=win7.jpg&w=200&h=200" /&gt;</pre>
    <div><img src="PreImg.ashx/lw?url=win7.jpg&w=200&h=200" /></div>
</body>
</html>
