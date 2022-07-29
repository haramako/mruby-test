* 引数のarray対応
* overload対応
* rubyのDictinay, ArrayなどをC#内で便利に使えるようにする
* CodeGenのコード整理: overload, 拡張メソッドなどの対応をしやすくするために、一度独自のクラスにクラスの情報をいれる
* a*の番号を1はじまりから、0はじまりにする

? ArrayをC#オブジェクトのまま返すか, RubyのArrayに変換するか・・・
? Value の継承を検討する(ArrayValue, DictionaryValue ...)

* ConverterをConverter,Utilに分割

* 例外まわりを整理する(C#の例外をrubyで補足、rubyの例外をC#で補足)
* GCまわりをちゃんとする
* generic対応 => Genericクラスだけ対応。Generic関数は無理か

* 拡張メソッドに対応 => いらん？
* ref, out, inに対応 => いらん？
* 予約語の回避(CodeGenerator.NormalName)
* Fiberのテスト
* bindingが対応する型を増やす
* 複数のMrbStateを使えるようにする
* CodeGenしなくても、リフレクションで触れる対応

- デフォルト引数に対応 =>いまは、一部の型のみ
- 引数がDLL.MRB_ARGS_OPT(4)固定になってるのを修正
- スタックトレースをだせるようにする
- Value classがmrbを持つように（いちいち、mrbを渡さなくて済むように）
- module, classをリストアップして、先に作るようにする
- 継承
- Send()がエラーを返した時の処理をちゃんとする
- mrb_funcall_argvが例外をabort()(==C#の例外)で処理するように変更
- 名前をスネークケースに
- 継承が動いていないのを修正
- mrb_load_string()がエラー時にnilを返すのが困る。raiseしてほしい
