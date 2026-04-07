    use super::*;

    struct DummyChannel;

    #[async_trait]
    impl Channel for DummyChannel {
        fn name(&self) -> &str {
            "dummy"
        }

        async fn send(&self, _message: &SendMessage) -> anyhow::Result<()> {
            Ok(())
        }

        async fn listen(
            &self,
            tx: tokio::sync::mpsc::Sender<ChannelMessage>,
        ) -> anyhow::Result<()> {
            tx.send(ChannelMessage {
                id: "1".into(),
                sender: "tester".into(),
                reply_target: "tester".into(),
                content: "hello".into(),
                channel: "dummy".into(),
                timestamp: 123,
                thread_ts: None,
                interruption_scope_id: None,
                attachments: vec![],
            })
            .await
            .map_err(|e| anyhow::anyhow!(e.to_string()))
        }
    }

    #[test]
    fn channel_message_clone_preserves_fields() {
        let message = ChannelMessage {
            id: "42".into(),
            sender: "alice".into(),
            reply_target: "alice".into(),
            content: "ping".into(),
            channel: "dummy".into(),
            timestamp: 999,
            thread_ts: None,
            interruption_scope_id: None,
            attachments: vec![],
        };

        let cloned = message.clone();
        assert_eq!(cloned.id, "42");
        assert_eq!(cloned.sender, "alice");
        assert_eq!(cloned.reply_target, "alice");
        assert_eq!(cloned.content, "ping");
        assert_eq!(cloned.channel, "dummy");
        assert_eq!(cloned.timestamp, 999);
    }

    #[tokio::test]
    async fn default_trait_methods_return_success() {
        let channel = DummyChannel;

        assert!(channel.health_check().await);
        assert!(channel.start_typing("bob").await.is_ok());
        assert!(channel.stop_typing("bob").await.is_ok());
        assert!(
            channel
                .send(&SendMessage::new("hello", "bob"))
                .await
                .is_ok()
        );
    }

    #[tokio::test]
    async fn default_reaction_methods_return_success() {
        let channel = DummyChannel;

        assert!(
            channel
                .add_reaction("chan_1", "msg_1", "\u{1F440}")
                .await
                .is_ok()
        );
        assert!(
            channel
                .remove_reaction("chan_1", "msg_1", "\u{1F440}")
                .await
                .is_ok()
        );
    }

    #[tokio::test]
    async fn default_draft_methods_return_success() {
        let channel = DummyChannel;

        assert!(!channel.supports_draft_updates());
        assert!(
            channel
                .send_draft(&SendMessage::new("draft", "bob"))
                .await
                .unwrap()
                .is_none()
        );
        assert!(channel.update_draft("bob", "msg_1", "text").await.is_ok());
        assert!(
            channel
                .finalize_draft("bob", "msg_1", "final text")
                .await
                .is_ok()
        );
        assert!(channel.cancel_draft("bob", "msg_1").await.is_ok());
    }

    #[tokio::test]
    async fn listen_sends_message_to_channel() {
        let channel = DummyChannel;
        let (tx, mut rx) = tokio::sync::mpsc::channel(1);

        channel.listen(tx).await.unwrap();

        let received = rx.recv().await.expect("message should be sent");
        assert_eq!(received.sender, "tester");
        assert_eq!(received.content, "hello");
        assert_eq!(received.channel, "dummy");
    }

    #[tokio::test]
    async fn default_redact_message_returns_success() {
        let channel = DummyChannel;

        assert!(
            channel
                .redact_message("chan_1", "msg_1", Some("spam".to_string()))
                .await
                .is_ok()
        );
        assert!(
            channel
                .redact_message("chan_1", "msg_2", None)
                .await
                .is_ok()
        );
    }
