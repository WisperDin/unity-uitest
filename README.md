# Unity UI测试自动化框架

//lzy

1.对框架的扩展在UITest.cs文件中
2.如果想写新的测试可以模仿Example中的UITestExample
3.我写的测试例子脚本名字为lzytestEx
4.像官方例子一样，然后在Reporting Test Runner组件下的Text Fliter 筛选想测试的脚本即可


# 特征

* 允许以类似于用户的方式编写驱动游戏的自动化测试
* 集成与Unity UI解决方案，可以轻松地与自定义解决方案，如NGUI或EZGUI集成
* 测试可以在编辑器或Unity播放器（测试Android，iOS和独立）
* 编辑器中的测试报告GUI，控制台和XML文件（JUnit格式）
* 包括用于对象嘲笑的轻量级依赖注入框架


# Running

* 要在编辑器中打开场景Assets / UITest / Example / TestRunner.unit运行测试，然后单击播放
* 单击TestRunner GameObject以实时查看测试报告
* 使用过滤器字段部分运行测试
* 要运行在设备上只是设置测试场景作为第一个建设


# 实现测试

* 要添加一个新测试，在项目扩展UITest的任何地方创建一个新类
* 使用与在单元测试中相同的方式使用UITest，UISetUp和UITearDown属性
* 在Assets / UITest / Examples / UITestExample.cs中的Checkout示例


# API

API被设计为可读的自然语言，所以它也可以被非技术人员理解。所有API调用都设计为等待其函数可以在一定的超时时间内执行。

* `Press（<GameObjectName>）` - 模拟按钮按。如果在场景中找不到具有给定名称的对象，它会等待它出现.
* `LoadScene（<SceneName>）` - 加载新场景并等待直到场景完全加载.
* `AssertLabel（<GameObjectName>，<Text>）` - 断言文本值，等待值更改.
* `WaitFor（<Condition>）` - 等待满足给定条件的通用方法.
* `WaitFor（new LabelTextAppeared（<GameObjectName>，<Text>））` - 等待带有给定文本的标签
* `WaitFor（new SceneLoaded（<SceneName>））` - 等到场景完全加载
* `WaitFor（new ObjectAppeared（<GameObjectName>））` - 等待具有给定名称的对象出现
* `WaitFor（new ObjectAppeared <ObjectType>（））` - 等待具有给定类型的组件的对象出现
* `WaitFor（new ObjectDisappeared（<GameObjectName>））` - 等待给定名称的对象消失
* `WaitFor（new ObjectDisappeared <ObjectType>（））` - 等待给定类型的组件消失的对象
* `WaitFor（new BoolCondition（<BoolFunction>））` - 当给定的bool表达式变为true时，通用条件被满足
