- module, classをリストアップして、先に作るようにする
* ConverterをConverter,Utilに分割

* rubyのDictinay, ArrayなどをC#内で便利に使えるようにする

* overload対応
* generic対応
* 引数がDLL.MRB_ARGS_OPT(4)固定になってるのを修正

* 予約語の回避(CodeGenerator.NormalName)
* Fiberのテスト
* bindingが対応する型を増やす
* スタックトレースをだせるようにする（デバッグビルド？）
- Value classがmrbを持つように（いちいち、mrbを渡さなくて済むように）

- 継承
- Send()がエラーを返した時の処理をちゃんとする
- mrb_funcall_argvが例外をabort()(==C#の例外)で処理するように変更
- 名前をスネークケースに
- 継承が動いていないのを修正

- mrb_load_string()がエラー時にnilを返すのが困る。raiseしてほしい
  