下面是项目开发完成后，反复验证工作流

- 开启claude code team模式，开启3个test agents，开启3个dev agents
- 参照任务列表进行端到端测试，发现问题交给dev agent修复，修复好后dev agent压缩上下文（只保留摘要），再由test agent回归测试
- 循环往复上面步骤，直至所有task测试通过，项目运行成功