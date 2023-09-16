# 死亡统计
记录每个玩家的死亡计数，并包括死亡来源，死亡时会报告计数。


# 本地记录
统计会记录在 ./tshock/DeathRecords.json
```json
{
  "records": {
    "玩家1": {
      "deathCounts": {
        "僵尸": 7,
        "绿史莱姆": 7,
        "史莱姆": 60
      }
    }
  }
}
```


## 如何使用
下载 [TerrariaDeathCounter.dll](https://github.com/hufang360/TShockDeathCounter/releases/download/20210310/TerrariaDeathCounter.dll) 并拷贝到 TShock 的 ServerPlugins 目录。


# Fork改动
Fork 至 [jamesrwaugh/TerrariaDeathCounter](https://github.com/jamesrwaugh/TerrariaDeathCounter)。
- 死亡报告改用中文描述，例如：“玩家1被史莱姆干掉了10次”；
- 不生成 DeathOutput.log；
- DeathRecords.json 将保存在 tshock目录下；
