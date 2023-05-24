![1684921517885](image/HashCode验证/1684921517885.png)


对齐HashCode ， 验证通过

本地检测不一致，会进行回滚


关键在于：ServerFrame 对象的比较。
本地存在ClientFrame、与实际接收的网络ServerFrame进行比较