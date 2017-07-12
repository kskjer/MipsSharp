# MipsSharp
This software works with N64 ROMs and binaries. It is capable of the following:

 - Detection of EUC-JP strings in a supplied file
 - File extraction from Zelda 64 ROMs
 - Zelda 64 Overlay disassembly
 - Function signature creation and identification

## Example: EUC-JP string detection

Many N64 games contain Japanese text encoded in EUC-JP. The following command will identify EUC-JP strings and output it in JSON (output has been trimmed for brevity):

```bash
$ MipsSharp --eucjp-strings --json --only-foreign --min-length=4 '0028 code'
[
  {
    "Filename": "0028 code",
    "Strings": [
    
      ... snip ...
      
      "マイクロコードが一致しなかった\n",
      "### TileSyncが必要です。\n",
      "### LoadSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "### PipeSyncが必要です。\n",
      "懲AUDIO : Ocarina Control Assign Normal\n",
      "酌gfxprint_open:２重オープンです\n",
      "\nダイナミックリンクファンクションのロードを開始します\n",
      "TEXT,DATA,RODATA+relをＤＭＡ転送します(%08x-%08x)\n",
      "リロケーションします\n",
      "BSS領域をクリアします(%08x-%08x)\n",
      "REL領域をクリアします(%08x-%08x)\n",
      "ダイナミックリンクファンクションのロードを終了します\n\n",
      "%s: %u バイトの%sに失敗しました\n",
      "%s: %u バイトの%sに成功しました\n",
      "システムヒープ表示\n",
      "\u001b[41;37m緊急事態！メモリリーク発見！ (block=%08x)\n\u001b[m",
      "\u001b[41;37m緊急事態！メモリリーク発見！ (block=%08x)\n\u001b[m",
      "\u001b[41;37m緊急事態！メモリリーク検出！ (block=%08x s=%08x e=%08x p=%08x)\n\u001b[m",
      "\u001b[41;37m__osFree:不正解放(%08x)\n\u001b[m",
      "\u001b[41;37m__osFree:二重解放(%08x)\n\u001b[m",
      "\u001b[41;37m__osFree:確保時と違う方法で解放しようとした (%08x:%08x)\n\u001b[m",
      "\u001b[41;37m__osFree:不正解放(%08x) [%s:%d ]\n\u001b[m",
      "\u001b[41;37m__osFree:二重解放(%08x) [%s:%d ]\n\u001b[m",
      "\u001b[41;37m__osFree:確保時と違う方法で解放しようとした (%08x:%08x)\n\u001b[m",
      "メモリブロックサイズが変わらないためなにもしません\n",
      "現メモリブロックの後ろにフリーブロックがあるので結合します\n",
      "新たにメモリブロックを確保して内容を移動します\n",
      "現メモリブロックの後ろのフリーブロックを大きくしました\n",
      "現メモリブロックの後ろにフリーブロックがないので生成します\n",
      "フリーブロック生成するだけの空きがありません\n",
      "アリーナは初期化されていません\n",
      "アリーナの内容 (0x%08x)\n",
      "メモリブロック範囲 status サイズ  [時刻  s ms us ns: TID:src:行]\n",
      "確保ブロックサイズの合計 0x%08x バイト\n",
      "空きブロックサイズの合計 0x%08x バイト\n",
      "最大空きブロックサイズ   0x%08x バイト\n",
      "アリーナの内容をチェックしています．．． (%08x)\n",
      "\u001b[41;37mおおっと！！ (%08x %08x)\n\u001b[m",
      "アリーナはまだ、いけそうです\n",
      "椀04 ",
      "経`6R",
      "淡`XR",
      " メッセージが,見つかった！！！ = %x  (data=%x) (data0=%x) (data1=%x) (data2=%x) (data3=%x)\n",
      " メッセージが,見つかった！！！ = %x  (data=%x) (data0=%x) (data1=%x) (data2=%x) (data3=%x)\n",
      " メッセージが,見つからなかった！！！ = %x\n",
      " メッセージが,見つかった！！！ = %x  (data=%x) (data0=%x) (data1=%x) (data2=%x) (data3=%x)\n",
      "録音開始 録音開始 録音開始 録音開始  -> ",
      "録音再生 録音再生 録音再生 録音再生  -> ",
      "８音録音開始 ８音録音開始 ８音録音開始  -> ",
      "８音再生 ８音再生 ８音再生  -> ",
      "輪唱開始 輪唱開始 輪唱開始 輪唱開始  -> ",
      "カエルの合唱 カエルの合唱  -> ",
      "オカリナ（%d） ",
      "タイマー (%x) (%x)",
      "合計wct=%x(%d)\n",
      "サウンド（ＳＥ）\n",
      "アイテム32-0\n",
      "アイテム24＝%d (%d) {%d}\n",
      "ＪＪ＝%d\n",
      "\n名前 ＝ ",
      "\nＥＶＥＮＴタイマー ＝ ",
      "\n流鏑馬スコア ＝ %d\n",
      "\n金スタ合計数 ＝ %d",
      "\n釣り堀魚サイズ ＝ ",
      "ランキング＝%d\n",
      "\nゼルダ時間 ＝ ",
      "？？？？？？？？？？？？？？？？  z_message.c  ？？？？？？？？？？？？？？？？？？\n",
      "吹き出し種類＝%d\n",
      "めっせーじ＝%x(%d)\n",
      "めっせーじ＝%x  message->msg_data\n",
      "\u001b[31m☆☆☆☆☆ オカリナ番号＝%d(%d) ☆☆☆☆☆\n\u001b[m",
      "オカリナモード = %d  (%x)\n",
      "演奏開始\n",
      "?????録音再生 録音再生 録音再生 録音再生  -> ",
      "台上演奏\n",
      "演奏チェック=%d\n",
      "Ocarina_Flog 正解模範演奏=%x\n",
      "Ocarina_Flog 正解模範演奏=%x\n",
      "Ocarina_Free 正解模範演奏=%x\n",
      "正解模範演奏=%x\n",
      "ここここここ\n",
      "キャンセル\n",
      "ocarina_no=%d  選曲=%d\n",
      "模範演奏=%x\n",
      "☆☆☆ocarina=%d   message->ocarina_no=%d  ",
      "Ocarina_PC_Wind=%d(%d) ☆☆☆   ",
      "Ocarina_C_Wind=%d(%d) ☆☆☆   ",
      "→  OCARINA_MODE=%d\n",
      "z_message.c 取得メロディ＝%d\n",
      "案山子録音 初期化\n",
      "    入力ボタン【%d】=%d",
      "録音終了！！！！！！！！！  message->info->status=%d \n",
      "録音終了！！！！！！！！！録音終了\n",
      "８音録音ＯＫ！\n",
      "すでに存在する曲吹いた！！！ \n",
      "輪唱失敗！！！！！！！！！\n",
      "輪唱成功！！！！！！！！！\n",
      " メッセージが,見つかった！！！ = %x\n",
      "\t,常駐ＰＡＲＡＭＥＴＥＲセグメント=%x\n",
      "ＤＯアクション テクスチャ初期=%x\n",
      "アイコンアイテム テクスチャ初期=%x\n",
      "ＥＶＥＮＴ＝%d\n",
      "タイマー停止！！！！！！！！！！！！！！！！！！！！！  = %d\n",
      "ＰＡＲＡＭＥＴＥＲ領域＝%x\n",
      "吹き出しgame_alloc=%x\n"
      
      ... snip ...
      
    ]
  }
]
```

## Example: Zelda 64 file extraction

The following example extracts the files from the specified ROM into the current directory.

```bash
$ MipsSharp --zelda64 -e "Zelda no Densetsu - Toki no Ocarina - Master Quest (J) (Debug Version).z64"

$ ls | head
0000 makerom
0001 boot
0002 dmadata
0003 Audiobank
0004 Audioseq
0005 Audiotable
0006 link_animetion
0007 icon_item_static
0008 icon_item_24_static
0009 icon_item_field_static
```

## Example: Zelda 64 overlay disassembly

The following example extracts and disassembles all overlays found in the supplied ROM. A folder is created for each overlay, 
which will contain the assembly source file as well as a linker script defining its entry point and external functions.

```bash
$ MipsSharp --zelda64 -A "Zelda no Densetsu - Toki no Ocarina - Master Quest (J) (Debug Version).z64"

$ ls | head
ovl_Arms_Hook
ovl_Arrow_Fire
ovl_Arrow_Ice
ovl_Arrow_Light
ovl_Bg_Bdan_Objects
ovl_Bg_Bdan_Switch
ovl_Bg_Bombwall
ovl_Bg_Bom_Guard
ovl_Bg_Bowl_Wall
ovl_Bg_Breakwall

$ ls -1 ovl_Arms_Hook
conf.ld
ovl_Arms_Hook.S
```

The disassembly output looks like this:

```
#include "mips.h"
#define s8 $fp

        #
        # .text         0x80864F00 - 0x80865AE0 (  2.97 kb)
        # .data         0x80865AE0 - 0x80865BC0 (  0.22 kb)
        # .rodata       0x80865BC0 - 0x80865C20 (  0.09 kb)
        # [relocs]      0x80865C20 - 0x80865D10 (  0.23 kb)
        # .bss          0x80865D10 - 0x80865D10 (  0.00 kb)
        #

        .set            noreorder
        .set            noat

        .text


        .global         func_80864F00
        .type           func_80864F00, @function

func_80864F00:
        jr              $ra
        sw              a1,532(a0)
data_80864F08:
        addiu           $sp,$sp,-40
        sw              s0,24($sp)
        or              s0,a0,$zero
        sw              a1,44($sp)
        or              a0,a1,$zero
        sw              $ra,28($sp)
        addiu           a1,s0,332
        jal             external_func_8005D018
        sw              a1,32($sp)
        lui             a3,%hi(data_80865B00)
        lw              a1,32($sp)
        addiu           a3,a3,%lo(data_80865B00)
        lw              a0,44($sp)
        jal             external_func_8005D104
        or              a2,s0,$zero
        lui             a1,%hi(data_80864FC4)
        addiu           a1,a1,%lo(data_80864FC4)
        jal             func_80864F00
        
        ... snip ...
```
